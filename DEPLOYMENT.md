# Web deployment

The game is an ASP.NET Core service. Kestrel serves the browser assets, command API, and server-sent event streams. Keep it on loopback behind the existing socket proxy and Apache TLS virtual host.

## Build and verify

```bash
./tools/build_web.sh
dotnet run --project tests/Backend/BackendTests.csproj
./tools/verify_web_rewrite.sh
dotnet publish src/OregonTrailDotNet.csproj -c Release -o ./publish
```

`build_web.sh` compiles nothing. It checks that the browser files exist and, if Node.js is installed, syntax-checks the scripts. The backend harness has no package dependencies beyond the game project itself.

`verify_web_rewrite.sh` builds the host from source, starts it on loopback, and checks the API contract, revision handling, the asset fingerprints, and the cache headers. It needs `dotnet`, `curl`, `node`, and `sha256sum` (or `shasum`). It tests the source tree, not `./publish`. The script lets the OS pick a free port, so it won't collide with a running service or with another run of itself. A few environment variables change its behavior:

- `WEB_VERIFY_PORT=18764` pins the port. The script fails if that port is already taken.
- `WEB_VERIFY_NO_RESTORE=1` skips the NuGet restore, so you can verify offline once packages are restored.
- `WEB_VERIFY_SKIP_NODE=1` skips the `tests/*.cjs` browser unit tests on a machine without Node.js. The run prints a warning and doesn't count as a release sign-off.

For the full HTTP integration checks, start a disposable host on a spare port (not 7682, which the production service uses) and run the suites against it from another terminal:

```bash
dotnet ./publish/OregonTrailDotNet.dll --urls http://127.0.0.1:18764
python3 tests/backend_integration.py http://127.0.0.1:18764
python3 tests/crypto_integration.py http://127.0.0.1:18764
python3 tests/creator_integration.py http://127.0.0.1:18764
python3 tests/food_sweep_integration.py http://127.0.0.1:18764
```

These create a lot of journeys and submit actions, so never point them at production. `GET /healthz` returns `{"status":"healthy"}` without touching the session registry, so it's safe as the production health check. `backend_integration.py` expects the default 12-hour cookie Max-Age. If the host runs with a different `SessionLifetime`, set `EXPECT_SESSION_MAX_AGE` to that lifetime in seconds.

### What ships

Everything in `src/wwwroot/` is published to `publish/wwwroot/`: the page, the scripts and stylesheets, `favicon.svg`, and the `fonts/` folder with the font license texts. Adding a file there is enough to ship it. The publish directory also contains the app (`OregonTrailDotNet.dll`, its dependencies, and a native launcher). The IIS `web.config` the Web SDK normally generates is turned off, since nothing here runs under IIS. The video ideas in `src/Module/Creator/video-ideas.json` are embedded in the dll and are not published as a separate file.

Deploy the whole publish directory at once to `/opt/oregon-trail-web/current`, so the server and browser code always come from the same release. The host looks for `wwwroot/index.html` next to the dll first, so the release directory serves its own assets whatever the working directory is.

## Proxy and service

The service runs the published dll on loopback:

```bash
dotnet /opt/oregon-trail-web/current/OregonTrailDotNet.dll --urls http://127.0.0.1:7682
```

The checked-in units provide:

- `deploy/oregon-proxy.socket`: loopback listener on port 7681.
- `deploy/oregon-proxy.service`: `systemd-socket-proxyd` forwarding to port 7682. It exits after 30 minutes with no open connections.
- `deploy/oregon-web.service`: the app, running as the unprivileged `oregon` user. It has `StopWhenUnneeded=yes`, so it stops when the proxy exits and starts again on the next request. That stop wipes every journey, since they all live in memory.
- `deploy/wagenhoffer-dev-le-ssl.conf`: the HTTPS virtual host with the `/oregon/` reverse proxy.

Apache strips `/oregon/` before forwarding, so the app sees requests at `/`. The browser uses relative URLs for assets, actions, and the stream, so routing never needs the prefix. Apache also sends `X-Forwarded-Prefix: /oregon`, which the app uses only to scope its session cookie.

The session cookie is `asphalt_trail_sid`: HttpOnly, SameSite=Lax, and `Secure` when the request is HTTPS. Its `Max-Age` is `SessionLifetime` (12 hours by default). Its `Path` is the forwarded prefix plus a slash, `/oregon/` here, so the cookie isn't sent to the rest of `wagenhoffer.dev`. Without the header, as in a direct local run, the path is `/`. The app only accepts a prefix made of one or more plain path segments (letters, digits, `-`, `.`, `_`, `~`; no `.` or `..` segment; at most 128 characters). It drops any other value, including percent escapes, `;`, commas, and repeated headers, so the cookie falls back to `Path=/` and the request still succeeds. A request with no cookie, or with a malformed one, gets a new session. `GET api/game` and the event stream re-send the existing cookie with a fresh `Max-Age`, and action posts send no `Set-Cookie`.

Releases before #63 used a cookie named `asphalt_trail_session` with `Path=/`. The app never reads that cookie. When a browser still sends it, the response expires it at `Path=/` and issues `asphalt_trail_sid` on the narrower path. Using a new name avoids a problem with keeping the old one: the browser would send both cookies under one name, and the `Cookie` header doesn't say which path each came from. A returning player starts a new journey, which they would anyway, since a release restarts the process.

SSE uses ordinary HTTP, without a WebSocket upgrade. The stream opens with `retry: 3000`, sends the current state right away, and sends `event: heartbeat` every 15 seconds while nothing changes. The browser gives up on a stream after 40 seconds with no state or heartbeat and reconnects. Keep proxy timeouts above that, and disable compression and buffering for the event path:

```apache
RewriteRule ^/oregon$ /oregon/ [R=301,L]
RewriteRule ^/Oregon/?$ /oregon/ [R=301,L]
ProxyPass        /oregon/ http://127.0.0.1:7681/ timeout=60
ProxyPassReverse /oregon/ http://127.0.0.1:7681/
<Location /oregon/>
    RequestHeader set X-Forwarded-Proto "https"
    RequestHeader set X-Forwarded-Prefix "/oregon"
</Location>
<Location /oregon/api/game/events>
    SetEnv no-gzip 1
</Location>
```

The app reads `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Prefix` and, with ASP.NET's defaults, only trusts them from loopback. A client can't spoof them through Apache: `RequestHeader set` replaces any value the client sent, and the app ignores these headers from any other address. Apache and the socket proxy both connect from 127.0.0.1, so the current setup works as is. Trusted proxies aren't a configuration setting. If the proxy ever moves to another machine, add its address to `KnownProxies` or `KnownNetworks` in `src/Program.cs`. Don't use `ASPNETCORE_FORWARDEDHEADERS_ENABLED` for this, because it clears the known-proxy lists and trusts the headers from any sender. The cookie is only marked `Secure` when the app sees the request as HTTPS, so it depends on that trusted `X-Forwarded-Proto`. Its path depends on the trusted `X-Forwarded-Prefix`: if Apache stops sending the header, the game still works but the cookie goes back to `Path=/`. If the public path ever changes, change the header to match the new `ProxyPass` prefix. Add the header to Apache before, or together with, the first release that reads it. The older app ignores it, while a newer app running without it issues `Path=/` cookies that stay in browsers until they expire.

### Caching and compression

- `GET /api/game` is `private, no-store` and has an ETag, so `If-None-Match` gets a 304 when nothing changed. The event stream is `private, no-cache, no-transform` with `X-Accel-Buffering: no`.
- `index.html` (served at `/`, `/index.html`, and any unknown path outside `/api/` that has no file extension) is `no-cache` with a weak ETag. The host rewrites each local `.css`, `.js`, and `.svg` reference in it to `name?v=<first 12 hex characters of the file's SHA-256>`, computed from the files on disk (`src/Web/BrowserShell.cs`).
- A request with an asset's current `?v=` gets `public, max-age=31536000, immutable`. Everything else under `wwwroot` gets `no-cache` and revalidates with its ETag. That includes the fonts, which are referenced from CSS rather than the page, and any request with no `?v=` or an old one.
- Don't type `?v=` values into `index.html` by hand. `verify_web_rewrite.sh` fails if you do.
- HTML, CSS, JS, and SVG are compressed with Brotli or gzip. Fonts and event streams aren't, and the stream is flushed after every write.

Before rollout:

```bash
systemd-analyze verify deploy/oregon-web.service deploy/oregon-proxy.service deploy/oregon-proxy.socket
apache2ctl configtest
```

Install the publish directory and the proxy configuration together, reload systemd, restart the app or socket as needed, and gracefully reload Apache. An open game stream counts as a connection, so it keeps the socket proxy from idling out. Closing or hiding the last tab closes the stream, and the journey's clock stops after `DisconnectGracePeriod` (30 seconds by default).

## Verify the release

```bash
curl --fail http://127.0.0.1:7681/healthz
curl --fail https://wagenhoffer.dev/oregon/healthz
curl --fail https://wagenhoffer.dev/oregon/
curl --fail https://wagenhoffer.dev/oregon/app.js
```

Check the session cookie. You should see one `asphalt_trail_sid` cookie with `max-age=43200` (or your `SessionLifetime` in seconds), `path=/oregon/`, `secure`, `samesite=lax`, and `httponly`:

```bash
curl --fail -s -o /dev/null -D - https://wagenhoffer.dev/oregon/api/game | grep -i '^set-cookie'
```

Check streaming with a disposable browser session. Keep its cookie between the bootstrap request and the stream request:

```bash
curl --fail -c /tmp/asphalt-check.cookies https://wagenhoffer.dev/oregon/api/game
curl --no-buffer --max-time 20 -b /tmp/asphalt-check.cookies https://wagenhoffer.dev/oregon/api/game/events
```

You should get one `event: state` right away and a heartbeat within 15 seconds, even on an idle menu. Curl exits on its 20-second timeout; that's expected. Actions in the same session should show up on the stream without another GET. Then do the desktop, mobile, and reconnect checks in [tests/WEB_E2E_CHECKLIST.md](tests/WEB_E2E_CHECKLIST.md).

## Capacity and lifecycle

These options live under `GameHost` in configuration. The repository has no `appsettings.json`, so the easiest way to change them is an environment variable in a systemd drop-in (`systemctl edit oregon-web.service`), for example `Environment=GameHost__MaximumSessions=512`. Command-line arguments such as `--GameHost:MaximumSessions=512` work too. The host checks the values at startup and refuses to start if any are out of range.

| Setting | Default | Allowed | Behavior |
| --- | --- | --- | --- |
| `MaximumSessions` | `256` | 1-4096 | New visitors get 503 with `Retry-After: 30` when full. Existing journeys are never evicted. |
| `CommandCapacity` | `32` | 1-1024 | Per-journey command queue. When it's full, requests get 429 with `Retry-After: 3`. |
| `MaximumStreamsPerSession` | `4` | 1-16 | More tabs than this get 429 on the stream and fall back to polling `GET /api/game` about every 3 seconds. |
| `SessionLifetime` | `12:00:00` | > 0 | A journey with no open stream and no requests for this long is removed. A sweep runs every 30 seconds. |
| `DisconnectGracePeriod` | `00:00:30` | >= 0 | Once no stream is open, the clock stops this long after the last request or stream close. |

The session cookie's Max-Age is `SessionLifetime`. Page loads, the browser's state reads (one every 15 seconds without a state event, or during fallback polling), and each stream connection renew it, so it slides with the journey's inactivity timeout instead of expiring a fixed time after the first visit. Request bodies are capped at 16 KB.

One worker owns each simulation, so commands and clock ticks never overlap within a journey, and separate journeys run independently. Reads and streams share one cached copy of each snapshot. A slow stream holds at most one pending snapshot and skips to the latest. Cancelling a request releases its stream. If a journey's game logic throws, only that journey stops: the next sweep removes it, and the browser starts a new journey when it reconnects. `expectedJourneyId` keeps a command written before a restart from applying to the new journey.

The simulation, scores, and tombstones are all **in memory**. They're lost when the process restarts (including the idle stop described above) or when a session expires. There are no saves to disk and no shared session store. If you run more than one replica, use sticky routing so a browser keeps reaching the process that owns its journey. Base `MaximumSessions` on measurements from the actual host.
