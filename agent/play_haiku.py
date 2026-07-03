#!/usr/bin/env python3
"""
Drive the REAL (unmodified) Asphalt Trail / Oregon Trail game with Claude Haiku.

How it works:
  - tmux gives the game a real PTY, so the unmodified console game runs (the
    "needs a TTY" limitation is satisfied; no headless host / code change needed).
  - We screen-scrape each frame with `tmux capture-pane -p`.
  - We hand the frame to `claude -p --model haiku` (already authenticated in this
    environment -- no API key needed) and ask for the literal keystrokes to type.
  - We type them back with `tmux send-keys`.

Usage:
  python3 agent/play_haiku.py [--turns N] [--model haiku] [--width 110] [--height 42]
"""
import argparse, subprocess, sys, time, re, os, datetime

REPO = "/root/mobile-dev/OregonTrail"
GAME_CMD = f"cd {REPO} && /root/.dotnet/dotnet bin/OregonTrailDotNet.dll"
SESSION = "asphalt"  # overwritten in main() when --tag is given

SYSTEM = """You are playing a text adventure called "The Asphalt Trail" (a re-skin of \
The Oregon Trail). You control the game by typing input at each screen.

INPUT RULES -- your reply must be ONLY the literal keystrokes to send, nothing else:
  * Menus list numbered choices ("1. ...", "2. ..."). To pick one, reply with just
    that number, e.g. 2
  * Some screens ask for free text (a traveler's name, an amount/quantity to buy).
    Reply with just that text or number, e.g. Dane  or  300
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
"""

def tmux(*args, check=False):
    return subprocess.run(["tmux", *args], capture_output=True, text=True)

def capture():
    return tmux("capture-pane", "-t", SESSION, "-p").stdout.rstrip("\n")

RAW = os.path.join(REPO, "agent", "game_raw.out")  # overwritten in main() when --tag is given

def start_game(width, height):
    tmux("kill-session", "-t", SESSION)
    open(RAW, "w").close()  # truncate
    tmux("new-session", "-d", "-s", SESSION, "-x", str(width), "-y", str(height),
         "bash", "-lc", GAME_CMD)
    # capture everything the pane renders (incl. a crash stack trace) to a raw file,
    # without disturbing the game's PTY
    tmux("pipe-pane", "-t", SESSION, "-o", f"cat >> {RAW}")

def session_alive():
    return tmux("has-session", "-t", SESSION).returncode == 0

def window_name(screen):
    m = re.search(r"Window\(\d+\):\s*(\w+)", screen)
    return m.group(1) if m else "?"

def wait_stable(prev_acted, settle=0.7, max_wait=10.0):
    """Wait until the frame stops changing (game is waiting for input).
    If it differs from prev_acted that's a fresh prompt; if it never settles we
    return the latest frame anyway."""
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
    # take the last non-empty line, strip quotes/backticks/whitespace
    lines = [l.strip() for l in reply.splitlines() if l.strip()]
    if not lines:
        return "ENTER"
    out = lines[-1].strip().strip("`'\"").strip()
    return out or "ENTER"

def ask_haiku(screen, model, session, first, timeout=150):
    """One continuous conversation so Haiku REMEMBERS what it already did.
    Turn 1 establishes the session (with the full rules); later turns resume it
    and send only the new screen."""
    if first:
        prompt = (f"{SYSTEM}\n\nWe begin now. Here is the first screen.\n"
                  f"----------\n{screen}\n----------\nReply with ONLY your input:")
        cmd = ["claude", "-p", "--session-id", session, "--model", model, prompt]
    else:
        prompt = (f"The screen is now:\n----------\n{screen}\n----------\n"
                  f"Reply with ONLY the keystrokes to type next:")
        cmd = ["claude", "-p", "--resume", session, "--model", model, prompt]
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout)
    except subprocess.TimeoutExpired:
        return "ENTER"
    return sanitize(r.stdout)

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
    ap.add_argument("--model", default="haiku")
    ap.add_argument("--width", type=int, default=110)
    ap.add_argument("--height", type=int, default=42)
    ap.add_argument("--tag", default="", help="suffix for tmux session/log/raw files, for running multiple in parallel")
    args = ap.parse_args()

    global SESSION, RAW
    if args.tag:
        SESSION = f"asphalt-{args.tag}"
        RAW = os.path.join(REPO, "agent", f"game_raw-{args.tag}.out")

    import uuid
    session = str(uuid.uuid4())
    ts = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    suffix = f"-{args.tag}" if args.tag else ""
    log_path = os.path.join(REPO, "agent", f"playthrough-{ts}{suffix}.log")
    log = open(log_path, "w")
    def emit(s):
        print(s, flush=True)
        log.write(s + "\n"); log.flush()

    emit(f"# Haiku playthrough {ts}  model={args.model}  session={session}")
    start_game(args.width, args.height)
    time.sleep(6)

    prev_acted = ""
    endgame_budget = -1   # >=0 once we hit GameOver: a few turns to ENTER through score
    for turn in range(1, args.turns + 1):
        if not session_alive():
            emit("\n[game process exited]")
            break
        screen = wait_stable(prev_acted)
        win = window_name(screen)
        emit("\n" + "=" * 70)
        emit(f"TURN {turn}  [window: {win}]")
        emit(screen)
        if win in ("GameOver", "Graveyard") and endgame_budget < 0:
            emit(f"\n[reached {win} -- playing through final screens]")
            endgame_budget = 6
        if endgame_budget >= 0:
            # End-game: just acknowledge screens so the score/rating renders, then stop.
            endgame_budget -= 1
            if endgame_budget < 0:
                emit("\n[end of game reached]")
                break
            send("ENTER")
            prev_acted = screen
            time.sleep(0.6)
            continue
        decision = ask_haiku(screen, args.model, session, first=(turn == 1))
        emit(f">>> HAIKU TYPES: {decision!r}")
        send(decision)
        prev_acted = screen
        time.sleep(0.6)

    # ---- final summary: pull score / rating / outcome from the transcript ----
    log.flush()
    with open(log_path) as f:
        text = f.read()
    emit("\n" + "#" * 70)
    emit("# OUTCOME")
    for kw in ["Greenhorn", "Adventurer", "TrailGuide", "points", "score",
               "died", "death", "survive", "Seattle", "arrived", "made it"]:
        for line in text.splitlines():
            if kw.lower() in line.lower() and "HAIKU TYPES" not in line and "STRATEGY" not in line:
                emit(f"  {line.strip()[:100]}")
                break

    emit(f"\n# done. transcript: {log_path}")
    log.close()

if __name__ == "__main__":
    main()
