#!/usr/bin/env python3
"""
Drive the REAL (unmodified) Asphalt Trail / Oregon Trail game with GLM (via opencode).

Mirrors agent/play_haiku.py (same tmux screen-scrape loop, same prompts, same
auto-handling of Graveyard/GameOver) -- only the LLM backend changes:

  claude -p --model haiku --session-id <uuid>   -->   opencode run --model opencode-go/glm-5.2 \
                                                          --format json --session <ses_xxx>

opencode idioms used (this is sst/opencode, NOT the old opencode-ai CLI):
  * The prompt is a positional argument: `opencode run "your prompt here"` (no -p).
  * --model takes provider/model form, e.g. --model opencode-go/glm-5.2.
  * There is NO stdin piping; everything goes in the single positional prompt.
  * --format json streams newline-delimited events; each carries a `sessionID`.
    We capture the sessionID from turn 1 and pass --session <id> on later turns so
    GLM remembers the running conversation (the opencode equivalent of
    `claude --resume <session>`).
  * (Optional) For lower per-call cold-start, start `opencode serve` once and add
    --attach http://localhost:4096. Not used by default because the swarm runner
    launches many processes in parallel and a shared server is awkward to manage.

Usage:
  python3 agent/play_glm.py [--turns N] [--model opencode-go/glm-5.2] \
                            [--width 110] [--height 42] [--tag STR] [--timeout 150] [--auto]
"""
import argparse, subprocess, sys, time, re, os, json, datetime, uuid

REPO = "/root/mobile-dev/OregonTrail"
GAME_CMD = f"cd {REPO} && /root/.dotnet/dotnet bin/OregonTrailDotNet.dll"
SESSION = "asphalt-glm"  # overwritten in main() when --tag is given

SYSTEM = """You are playing a text adventure called "The Asphalt Trail" (a re-skin of \
The Oregon Trail). You control the game by typing input at each screen.

INPUT RULES -- your reply must be ONLY the literal keystrokes to send, nothing else:
  * Menus list numbered choices ("1. ...", "2. ..."). To pick one, reply with just
    that number, e.g. 2
  * Some screens ask for free text (a traveler's name, an amount/quantity to buy).
    Reply with just that text or number, e.g. Dane  or  300
  * When a screen asks for a QUANTITY or AMOUNT, reply with ONLY a single plain whole
    number like 20 or 300. NEVER a range ("1-100"), a symbol ("+"), or words.
  * The store screen lists items "1. Gas Cans ...", "2. Snacks ...", etc.
    CONFIRM what the screen shows before typing:
      - The line beginning with ">" is ALREADY selected (the cursor). Look at its
        quantity ("x20") and the "Total bill" line.
      - To buy the highlighted item at its shown quantity, reply: ENTER
      - To move the cursor to a different item (e.g. Snacks), reply with that
        item's NUMBER (e.g. 2). Only after the cursor is on the right item do you
        press ENTER to add it to the bill.
      - To lower/raise the highlighted item's quantity, the store uses Left/Right
        arrows; do NOT reply with a quantity number like "200" -- it is ignored
        because there is no item #200.
      - When "Total bill" is non-zero and you have what you want, type 8 to leave
        the store and confirm the purchase.
    Do NOT loop: at most a handful of ENTERs, then 8. Never type "DOWN", "UP",
    "LEFT", "RIGHT", or a multi-digit quantity.
  * Many screens just say to press ENTER / RETURN to continue, or show a message
    with no menu. In that case reply with the single word: ENTER
  * Never explain. Never use punctuation or quotes around your answer. Output one
    line containing only what should be typed.

CRITICAL -- avoid loops. You have NO memory of previous screens. NEVER choose an
informational option such as "Learn about...", "Find out the differences",
"About", or "See the differences" -- they just loop you back here and waste turns.
ALWAYS pick an option that makes real progress (start, choose, buy, continue, leave).

YOUR GOAL IS TO WIN: get all 4 travelers to Seattle alive with the highest score
you can. Play deliberately toward that, every turn. Concretely:
  * When asked to choose a profession/life, just pick one (1, 2, or 3) and commit;
    do NOT pick the "find out / differences" option.
  * In the store, spend big on the two things that win the game: buy the MOST fuel
    (gas) you can, and a large food supply -- running out of either kills the party.
    Grab a couple of spare parts/tires too if affordable. Then leave the store.
  * On the road, keep moving ("continue on the trail"). Check status occasionally.
  * Keep everyone fed -- do NOT starve the party, and do NOT over-ration; eat a
    normal/filling ration. Rest when someone's health drops to fair/poor.
  * At rivers/hazards, when the water is deep or it's flagged dangerous, take the
    SAFE option (pay for the safe convoy / wait it out) rather than gambling the SUV.
  * Never quit, never pick "End"/"go back to the start" while the party is alive.

Do NOT call any tools. Just reply with the keystrokes as plain text.
"""

def tmux(*args):
    return subprocess.run(["tmux", *args], capture_output=True, text=True)

def capture():
    return tmux("capture-pane", "-t", SESSION, "-p").stdout.rstrip("\n")

RAW = os.path.join(REPO, "agent", "game_raw-glm.out")  # overwritten in main() when --tag is given

def start_game(width, height):
    tmux("kill-session", "-t", SESSION)
    open(RAW, "w").close()
    tmux("new-session", "-d", "-s", SESSION, "-x", str(width), "-y", str(height),
         "bash", "-lc", GAME_CMD)
    tmux("pipe-pane", "-t", SESSION, "-o", f"cat >> {RAW}")

def session_alive():
    return tmux("has-session", "-t", SESSION).returncode == 0

def window_name(screen):
    m = re.search(r"Window\(\d+\):\s*(\w+)", screen)
    return m.group(1) if m else "?"

def wait_stable(prev_acted, settle=0.7, max_wait=10.0):
    start = time.time()
    last = capture()
    while time.time() - start < max_wait:
        time.sleep(settle)
        cur = capture()
        if cur == last:
            return cur
        last = cur
    return last

def sanitize(reply):
    lines = [l.strip() for l in reply.splitlines() if l.strip()]
    if not lines:
        return "ENTER"
    out = lines[-1].strip().strip("`'\"").strip()
    return out or "ENTER"

def ask_glm(screen, model, session_id, first, timeout, auto, attach):
    """One continuous opencode session so GLM remembers its prior moves.

    Turn 1: no --session (opencode mints a new ses_xxx); we parse its ID from the
            --format json stream and reuse it via --session on every later turn.
    Later turns: pass --session <id> so the conversation continues in-place.
    Returns (decision_string, session_id)."""
    if first:
        prompt = (f"{SYSTEM}\n\nWe begin now. Here is the first screen.\n"
                  f"----------\n{screen}\n----------\nReply with ONLY your input:")
        cmd = ["opencode", "run", "--model", model, "--format", "json"]
        if attach:
            cmd += ["--attach", attach]
        if auto:
            cmd.append("--auto")
        cmd.append(prompt)
    else:
        prompt = (f"The screen is now:\n----------\n{screen}\n----------\n"
                  f"Reply with ONLY the keystrokes to type next:")
        cmd = ["opencode", "run", "--model", model, "--format", "json",
               "--session", session_id]
        if attach:
            cmd += ["--attach", attach]
        if auto:
            cmd.append("--auto")
        cmd.append(prompt)

    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
    except subprocess.TimeoutExpired:
        return "ENTER", session_id  # leave session_id unchanged so we can resume

    text_parts = []
    sid = session_id
    for line in r.stdout.splitlines():
        line = line.strip()
        if not line or not line.startswith("{"):
            continue
        try:
            ev = json.loads(line)
        except json.JSONDecodeError:
            continue
        if "sessionID" in ev and not sid:
            sid = ev["sessionID"]
        # The model's textual answer is streamed as {"type":"text","part":{"text":...}}
        if ev.get("type") == "text" and isinstance(ev.get("part"), dict):
            t = ev["part"].get("text")
            if t:
                text_parts.append(t)
    return sanitize("".join(text_parts)), (sid or session_id)

def send(keys):
    if keys.upper() == "ENTER":
        tmux("send-keys", "-t", SESSION, "Enter")
    else:
        tmux("send-keys", "-t", SESSION, "-l", keys)
        time.sleep(0.15)
        tmux("send-keys", "-t", SESSION, "Enter")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--turns", type=int, default=400)
    ap.add_argument("--model", default="opencode-go/glm-5.2",
                    help="opencode provider/model, e.g. opencode-go/glm-5.2 (or zai/glm-5.2)")
    ap.add_argument("--width", type=int, default=110)
    ap.add_argument("--height", type=int, default=42)
    ap.add_argument("--tag", default="glm",
                    help="suffix for tmux session/log/raw files (allows parallel runs)")
    ap.add_argument("--timeout", type=int, default=150,
                    help="per-turn opencode run timeout in seconds")
    ap.add_argument("--auto", action="store_true",
                    help="pass --auto to opencode (auto-approve tool permissions; dangerous)")
    ap.add_argument("--attach", default=os.environ.get("OPENCODE_ATTACH", ""),
                    help="opencode server URL (e.g. http://localhost:4096) to reuse a warm "
                         "session -- avoids per-call MCP cold-start. Run `opencode serve` once, "
                         "then pass --attach or export OPENCODE_ATTACH=...")
    args = ap.parse_args()

    global SESSION, RAW
    if args.tag:
        SESSION = f"asphalt-{args.tag}"
        RAW = os.path.join(REPO, "agent", f"game_raw-{args.tag}.out")

    session_id = ""  # opencode assigns ses_xxx on turn 1
    ts = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    suffix = f"-{args.tag}" if args.tag else ""
    log_path = os.path.join(REPO, "agent", f"playthrough-{ts}{suffix}.log")
    log = open(log_path, "w")
    def emit(s):
        print(s, flush=True)
        log.write(s + "\n"); log.flush()

    emit(f"# GLM playthrough {ts}  model={args.model}  tag={args.tag}")
    start_game(args.width, args.height)
    time.sleep(6)

    prev_acted = ""
    endgame_budget = -1
    for turn in range(1, args.turns + 1):
        if not session_alive():
            emit("\n[game process exited]")
            break
        screen = wait_stable(prev_acted)
        win = window_name(screen)
        emit("\n" + "=" * 70)
        emit(f"TURN {turn}  [window: {win}]")
        emit(screen)
        if win == "GameOver" and endgame_budget < 0:
            emit(f"\n[reached {win} -- playing through final screens]")
            endgame_budget = 6
        if endgame_budget >= 0:
            endgame_budget -= 1
            if endgame_budget < 0:
                emit("\n[end of game reached]")
                break
            send("ENTER")
            prev_acted = screen
            time.sleep(0.6)
            continue
        if win == "Graveyard":
            if re.search(r"Y\s*/\s*N", screen) or "2. No" in screen:
                send("2")
            else:
                send("ENTER")
            prev_acted = screen
            time.sleep(0.5)
            continue
        decision, session_id = ask_glm(screen, args.model, session_id,
                                      first=(turn == 1), timeout=args.timeout,
                                      auto=args.auto, attach=args.attach)
        if turn == 1:
            emit(f"[opencode session: {session_id}]")
        emit(f">>> GLM TYPES: {decision!r}")
        send(decision)
        prev_acted = screen
        time.sleep(0.6)

    log.flush()
    with open(log_path) as f:
        text = f.read()
    emit("\n" + "#" * 70)
    emit("# OUTCOME")
    for kw in ["Greenhorn", "Adventurer", "TrailGuide", "points", "score",
               "died", "death", "survive", "Seattle", "arrived", "made it"]:
        for line in text.splitlines():
            if kw.lower() in line.lower() and "GLM TYPES" not in line and "STRATEGY" not in line:
                emit(f"  {line.strip()[:100]}")
                break

    emit(f"\n# done. transcript: {log_path}")
    log.close()

if __name__ == "__main__":
    main()