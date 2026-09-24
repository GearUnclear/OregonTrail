# Web deployment

The game is an ASP.NET Core service. Kestrel serves the browser assets, command API, and server-sent event streams. Keep it on loopback behind the existing socket proxy and Apache TLS virtual host.

## Build and verify

```bash
./tools/build_web.sh
dotnet run --project tests/Backend/BackendTests.csproj
./tools/verify_web_rewrite.sh
dotnet publish src/OregonTrailDotNet.csproj -c Release -o ./publish
```

For the full HTTP integration check, run a disposable published host and, in another terminal:

```bash
python3 tests/backend_integration.py http://127.0.0.1:7682
```

The checks create multiple journeys and submit actions; use a test host. `GET /healthz` is the production health check and allocates no journey. With dependencies already restored, `WEB_VERIFY_NO_RESTORE=1 ./tools/verify_web_rewrite.sh` permits offline verification. The worker harness has no extra package dependencies.

Publish `wwwroot/index.html`, `styles.css`, `bridge.js`, `scenes.js`, `app.js`, and `favicon.svg` with the server. Deploy the complete publish directory atomically at `/opt/oregon-trail-web/current`, so the action contract and browser transport always come from the same release.

## Proxy and service

```bash
dotnet ./publish/OregonTrailDotNet.dll --urls http://127.0.0.1:7682
```

The checked-in units provide:

- `deploy/oregon-proxy.socket`: loopback listener on port 7681.
- `deploy/oregon-proxy.service`: socket proxy to port 7682, with a 30-minute idle timeout.
- `deploy/oregon-web.service`: unprivileged application process.
- `deploy/wagenhoffer-dev-le-ssl.conf`: HTTPS `/oregon/` reverse proxy.

Apache strips `/oregon/`; the browser uses relative asset, command, and stream URLs. SSE uses ordinary HTTP, without a WebSocket upgrade. The stream sends a heartbeat every 15 seconds. Keep proxy timeouts above that interval and disable compression/buffering for the event path:

```apache
RewriteRule ^/oregon$ /oregon/ [R=301,L]
ProxyPass        /oregon/ http://127.0.0.1:7681/ timeout=60
ProxyPassReverse /oregon/ http://127.0.0.1:7681/
<Location /oregon/>
    RequestHeader set X-Forwarded-Proto "https"
</Location>
<Location /oregon/api/game/events>
    SetEnv no-gzip 1
</Location>
```

ASP.NET trusts loopback forwarded headers by default. If the proxy moves to another host, explicitly configure its trusted address before forwarding scheme/client headers. Secure cookies rely on the trusted HTTPS scheme. Dynamic state is private and not cacheable; snapshots support conditional GET with ETags. Static assets require cache revalidation and support Brotli/gzip. Event streams flush immediately and are never compressed by the host.

Before rollout:

```bash
systemd-analyze verify deploy/oregon-web.service deploy/oregon-proxy.service deploy/oregon-proxy.socket
apache2ctl configtest
```

Install the publish directory and proxy configuration together, reload systemd, restart the app/socket as appropriate, and gracefully reload Apache. An open game stream prevents the socket proxy's idle shutdown. Closing or hiding the last tab disconnects the stream; simulation pulses stop after a 30-second grace period.

## Verify the release

```bash
curl --fail http://127.0.0.1:7681/healthz
curl --fail https://wagenhoffer.dev/oregon/healthz
curl --fail https://wagenhoffer.dev/oregon/
curl --fail https://wagenhoffer.dev/oregon/app.js
```

Check streaming with a disposable browser session, preserving its cookie between the bootstrap and stream request:

```bash
curl --fail -c /tmp/asphalt-check.cookies https://wagenhoffer.dev/oregon/api/game
curl --no-buffer --max-time 20 -b /tmp/asphalt-check.cookies https://wagenhoffer.dev/oregon/api/game/events
```

Expect one initial `event: state` promptly and a heartbeat within 15 seconds even on an unchanged menu. Curl's timeout exit at 20 seconds is intentional. Actions in that same session must appear on the stream without another GET. Complete the desktop/mobile and reconnect checks in [tests/WEB_E2E_CHECKLIST.md](tests/WEB_E2E_CHECKLIST.md).

## Capacity and lifecycle

Configure these with appsettings or environment variables, for example `GameHost__MaximumSessions=256`:

| Setting | Default | Behavior |
| --- | --- | --- |
| `MaximumSessions` | 256 | New visitors receive 503 at capacity; existing journeys are never evicted. |
| `CommandCapacity` | 32 | Per-journey mailbox; overload receives 429. |
| `MaximumStreamsPerSession` | 4 | Additional live tabs receive 429 and use bounded polling fallback. |
| `SessionLifetime` | `12:00:00` | Inactive journeys are removed by a 30-second sweep. |
| `DisconnectGracePeriod` | `00:00:30` | Game pauses after the last stream/request becomes inactive. |

One worker owns each simulation, so mutations and clock pulses cannot overlap within a journey. Different journeys run independently. Reads and subscribers share serialized immutable publications; each subscriber buffers at most one full state. Cancellation releases stream subscriptions. A failed domain operation stops only that journey; the registry removes it on its next sweep, and reconnect establishes a new incarnation. `expectedJourneyId` prevents a command prepared before a restart from mutating the replacement journey.

The simulation, scores, and tombstones are **in memory**. They disappear on process restart or session expiry. This release improves concurrency and live transport; it does not add durable saves or a distributed session store. Replicas require sticky routing to keep one browser on its owning process. Size the configurable admission limit with measurements on the actual deployment host.
