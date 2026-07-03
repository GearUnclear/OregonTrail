# Web deployment: wagenhoffer.dev/oregon

The game is exposed as a public, single-player-at-a-time web terminal at
`https://wagenhoffer.dev/oregon`. None of the files described here live in
this git repo — they're server state on the box that hosts wagenhoffer.dev.
This doc is the record of what was set up and why, so it can be reproduced
or debugged without re-deriving it from scratch.

## Why a web terminal, not a rewrite

`Program.cs` is a hard requirement on a real TTY (`Console.KeyAvailable`,
`Console.ReadKey`, `Console.WindowHeight/Width`, cursor positioning — see
`CLAUDE.md`). Rather than porting the WolfCurses render loop to HTML/JS, the
game runs unmodified inside a real PTY on the server, and that PTY is
streamed to the browser over WebSocket via **ttyd**. The browser gets an
xterm.js terminal; the server-side process is the exact same console binary
`dotnet build` produces.

## Why single-session, not per-visitor

WolfCurses is a singleton (`GameSimulationApp.Instance`) with no concept of
concurrent games — the whole design assumes one player, one process. Rather
than fight that, each web visitor gets their own OS process, but only one is
allowed to exist at a time. A visitor who connects while a game is in
progress sees a "currently occupied, refresh to try again" message and is
disconnected — they are not queued, buffered, or shown a waiting room; the
mechanism really is "try again by reloading."

## Architecture

```
Browser
  │ HTTPS/WSS
  ▼
Cloudflare (proxied DNS, wagenhoffer.dev)
  │
  ▼
Apache (already fronts wagenhoffer.dev + other subdomains on this box)
  │ ProxyPass /oregon + websocket upgrade rule
  ▼
ttyd (127.0.0.1:7681, base-path /oregon) — systemd service, runs as
  low-privilege user "oregon"
  │ spawns one process per WebSocket connection
  ▼
/opt/oregon-trail-web/play.sh
  │ flock -n on /run/oregon-trail-web/game.lock
  ├─ lock free  → exec dotnet OregonTrailDotNet.dll (the game)
  └─ lock held  → print busy message, sleep 3, exit 1
```

## Components

### 1. Deployed game bin (`/opt/oregon-trail-web/bin/`)

A copy of the build output — **not** a symlink into the dev checkout at
`/root/mobile-dev/OregonTrail/bin/`. Two reasons:

- `/root` is `700`, unreadable by any non-root user, so a low-priv service
  account can't traverse into it. Copying the build artifacts to `/opt`
  (world-readable, `755`) avoids granting ACL access into `/root` (the
  original plan — see "Security decisions" below).
- Decouples the running production copy from the dev tree: rebuilding for
  local development doesn't affect the live site, and vice versa.

To redeploy after a code change:

```bash
cd /root/mobile-dev/OregonTrail
dotnet build src/OregonTrailDotNet.csproj -o ./bin
cp bin/OregonTrailDotNet.dll bin/OregonTrailDotNet.deps.json \
   bin/OregonTrailDotNet.runtimeconfig.json bin/WolfCurses.dll \
   /opt/oregon-trail-web/bin/
```

No service restart needed — each new connection spawns a fresh process that
picks up the new binaries; only in-flight games keep running the old code
until they exit.

### 2. Runtime version mismatch (.NET 6 target, .NET 8 runtime)

The project targets `net6.0`, but Ubuntu 24.04's apt repos only carry
`dotnet-runtime-8.0`/`-10.0` — no 6.0 package. Rather than add Microsoft's
third-party apt feed just for this, the deployed `runtimeconfig.json` is run
with roll-forward enabled:

```bash
DOTNET_ROLL_FORWARD=LatestMajor dotnet /opt/oregon-trail-web/bin/OregonTrailDotNet.dll
```

This is Microsoft's supported mechanism for running an app built against an
older target framework on a newer installed runtime (no source/config
changes to the `.csproj`, since that's the dev repo's own concern — the
env var is set in `play.sh`, not baked into `runtimeconfig.json`). Verified
working under `dotnet-runtime-8.0` before wiring up the rest of the stack.

### 3. Wrapper script (`/opt/oregon-trail-web/play.sh`)

```bash
#!/bin/bash
LOCKFILE=${RUNTIME_DIRECTORY:-/run/oregon-trail-web}/game.lock
GAME_DIR=/opt/oregon-trail-web
DOTNET_BIN=/usr/bin/dotnet
GAME_DLL=$GAME_DIR/bin/OregonTrailDotNet.dll
DOTNET_ROLL_FORWARD=LatestMajor
export DOTNET_ROLL_FORWARD

exec 9>"$LOCKFILE"

if ! flock -n 9; then
    cat <<'EOF'


   ============================================
     THE ASPHALT TRAIL is currently occupied.

     Only one wagon party can be on the road
     at a time. Someone else is already playing.

     Close this tab and refresh the page to
     check again in a few minutes.
   ============================================


EOF
    sleep 3
    exit 1
fi

cd "$GAME_DIR" || exit 1
exec "$DOTNET_BIN" "$GAME_DLL"
```

This is what ttyd runs per connection (`ttyd ... /opt/oregon-trail-web/play.sh`).
Mechanics worth noting:

- `flock -n 9` is non-blocking — it fails immediately rather than queuing,
  which is what turns "someone else is playing" into an instant message
  instead of a hang.
- The lock is tied to file descriptor 9, opened via `exec 9>"$LOCKFILE"`.
  Bash's `exec` (the final line, replacing the shell with the `dotnet`
  process) does **not** close file descriptors on exec unless they're
  marked close-on-exec, so fd 9 — and the lock — carries over into the
  `dotnet` process. The lock releases automatically the instant that
  process exits for any reason (game completion, ttyd killing it on
  disconnect, crash), with no explicit cleanup code needed.
- `RUNTIME_DIRECTORY` is a systemd-injected env var (see the unit below);
  falls back to `/run/oregon-trail-web` if run manually outside systemd.

### 4. systemd unit (`/etc/systemd/system/oregon-ttyd.service`)

```ini
[Unit]
Description=ttyd web terminal for The Asphalt Trail (wagenhoffer.dev/oregon)
After=network.target

[Service]
Type=simple
User=oregon
Group=oregon
RuntimeDirectory=oregon-trail-web
RuntimeDirectoryMode=0750
ExecStart=/usr/bin/ttyd -i 127.0.0.1 -p 7681 -W -T xterm-256color -b /oregon /opt/oregon-trail-web/play.sh
Restart=always
RestartSec=2
NoNewPrivileges=true
ProtectSystem=strict
ProtectHome=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
```

- `-i 127.0.0.1` — loopback only; never directly reachable from the
  internet, only via the Apache reverse proxy.
- `-b /oregon` — ttyd's base-path flag, so its HTML/JS assets and WebSocket
  endpoint are served at `/oregon` and `/oregon/ws` to match the Apache
  proxy path.
- `-W` — writable (client keystrokes reach the PTY; ttyd defaults to
  read-only).
- `RuntimeDirectory=oregon-trail-web` — systemd creates and owns
  `/run/oregon-trail-web` (mode 0750, owned by `oregon:oregon`) for the
  lifetime of the service; this is what makes `ProtectSystem=strict` (which
  otherwise makes the whole filesystem read-only to the service) compatible
  with needing a writable lock file.
- `ProtectHome=true` — `/root`, `/home`, `/run/user` are inaccessible to
  this service (`ENOENT`), which is fine since nothing under `/opt` needs
  them anymore.

`apt install ttyd` auto-enables a *different*, stock `ttyd.service` that
runs a bare `login` shell on `127.0.0.1:7681` with no auth
(`/etc/default/ttyd`: `TTYD_OPTIONS="-i lo -p 7681 -O login"`). That stock
unit was disabled (`systemctl disable --now ttyd`) before this one was
created, since they'd otherwise fight over port 7681.

### 5. Apache reverse proxy

Added to `/etc/apache2/sites-available/wagenhoffer-dev-le-ssl.conf` (the
existing HTTPS vhost for `wagenhoffer.dev`, alongside its pre-existing
`/login` honeypot):

```apache
# The Asphalt Trail - single-session web terminal (ttyd on 127.0.0.1:7681)
RewriteCond %{HTTP:Upgrade} =websocket [NC]
RewriteRule ^/oregon/(.*) ws://127.0.0.1:7681/oregon/$1 [P,L]

ProxyPass /oregon http://127.0.0.1:7681/oregon
ProxyPassReverse /oregon http://127.0.0.1:7681/oregon
```

The websocket-upgrade `RewriteRule` must come before the plain `ProxyPass`
so `Upgrade: websocket` requests get proxied with the `ws://` scheme (needed
for `mod_proxy_wstunnel`) while everything else (the ttyd HTML/JS page) goes
through the ordinary HTTP proxy. Required `mod_proxy_wstunnel`
(`a2enmod proxy_wstunnel`) in addition to the `proxy`/`proxy_http` modules
that were already enabled.

Applied with `apache2ctl graceful` (not `restart`), since this box's Apache
also serves `ai.`, `blog.`, `cloud.`, `music.`, and `wiki.wagenhoffer.dev` —
graceful reload picks up new config/modules without dropping in-flight
connections to those.

The plain-HTTP vhost (`wagenhoffer-dev.conf`) needed no changes — it already
unconditionally redirects everything to HTTPS.

## Security decisions

- **No `/root` access.** The original plan referenced the dev SDK install at
  `/root/.dotnet` and the dev checkout at `/root/mobile-dev/OregonTrail`
  directly, via an ACL grant (`setfacl -m u:oregon:--x /root`) letting a new
  low-priv user traverse into `/root`. That was flagged and blocked by the
  permission system as an unrequested, security-relevant escalation. Instead:
  a system `.NET` runtime was installed via apt (`dotnet-runtime-8.0`, see
  above) and the built game was copied out to `/opt/oregon-trail-web`, so
  the service never touches `/root` at all — `ProtectHome=true` enforces
  this at the systemd level too.
- **Dedicated low-privilege user.** `useradd --system --no-create-home
  --shell /usr/sbin/nologin oregon`. The service runs as this user, not
  root, not `www-data`. This is a public, unauthenticated, internet-facing
  process spawning an interactive shell-adjacent program per connection —
  worth the isolation even though the game itself has no known file I/O or
  shell-escape surface (per `CLAUDE.md`, saves/scores are in-memory only,
  nothing is written to disk by game logic).
- **Hardened unit.** `NoNewPrivileges`, `ProtectSystem=strict`,
  `ProtectHome=true`, `PrivateTmp=true` on top of the low-priv user.
- **Loopback-only ttyd.** Only reachable through the Apache proxy; never
  bound to a public interface.

## Verification performed

- Confirmed the game runs correctly under a real PTY (`tmux`) before
  touching any web infra.
- Confirmed `dotnet-runtime-8.0` + `DOTNET_ROLL_FORWARD=LatestMajor` runs
  the `net6.0`-targeted build correctly.
- Confirmed the low-priv `oregon` user can read/execute everything needed
  under `/opt/oregon-trail-web` with no changes to `/root` permissions
  (`sudo -u oregon test -r ...`, `sudo -u oregon /opt/oregon-trail-web/play.sh`).
- Confirmed the lock logic directly (two concurrent `tmux` sessions running
  `play.sh`): first acquires and runs the game, second gets the busy message
  and exits, and a third connection succeeds again once the first exits.
- Confirmed `apache2ctl configtest` passes and the other five vhosts on this
  box (`ai.`, `blog.`, `cloud.`, `music.`, `wiki.wagenhoffer.dev`) still
  respond after the `graceful` reload.
- Confirmed the full public path end-to-end (Cloudflare → Apache → ttyd)
  with two concurrent raw WebSocket clients (Python, `websockets` library)
  hitting `wss://wagenhoffer.dev/oregon/ws`: the first receives real game
  output, the second receives the exact busy-message text before its socket
  closes.
- Confirmed no orphaned game processes or stale locks were left behind after
  testing (`ps`, `flock -n` probe on the lock file).

## Operational notes

- **Logs:** `journalctl -u oregon-ttyd`, plus Apache's existing
  `wagenhoffer_access.log` / `wagenhoffer_error.log`.
- **Status:** `systemctl status oregon-ttyd`.
- **Force-clear a stuck lock** (e.g. after a crash that somehow left a
  process alive holding it): find and kill the `dotnet
  /opt/oregon-trail-web/bin/OregonTrailDotNet.dll` process; the lock releases
  when its fd 9 closes. Deleting `/run/oregon-trail-web/game.lock` itself
  does nothing useful while a process still holds the flock on the open fd.
- **The service restarts automatically** (`Restart=always`) if ttyd itself
  crashes; individual game processes are unaffected by that (they're
  separate child processes, one per connection).
