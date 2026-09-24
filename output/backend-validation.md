# Backend rewrite validation — 2026-09-23

The final published application was exercised on temporary loopback hosts. Production services were not changed.

## Architecture and performance

- Each journey runs behind its own bounded command mailbox and execution-local game context. The registry lock only admits/removes workers.
- Readers share an immutable serialized snapshot; subscribers buffer at most the newest full state. Idle workers sleep until activity resumes.
- Sixteen concurrent fresh journeys: baseline **17,419 ms**, first rewritten-host measurement **1,762 ms**. Later published-release integration runs were **1,072–1,345 ms**. These are development-host measurements, not a capacity guarantee.
- The same threaded client measured warm GET median/P95 of **6.3/14.7 ms** before and **6.1/22.0 ms** after in its first run; this check does not establish an improvement in warm-request latency.
- A healthy browser made **zero recurring snapshot GETs** during the observed idle window. State changes arrived through SSE.

## Passed

- Release build/publish and browser JavaScript syntax checks.
- Worker harness: bounded command queue, canceled queued actions, independent workers, cached reads, bounded subscriber fanout, incarnation guard, active/idle lifecycle, capacity without eviction, graceful shutdown, and cancellation during startup.
- HTTP integration: health without session allocation, 16 independent concurrent sessions, ETags, pushed actions, stale revision/incarnation rejection, stream reconnect, stream admission/cleanup, session exit/restart, and concurrent command conflicts.
- Four parallel full journey setups with distinct vehicles and passenger names, validated supply purchases, parked state, and streamed driving start/stop.
- Real Chromium: setup, keyboard submission, input/focus preservation, store quantities/checkout, zero idle polling, parked and moving frames, reduced motion, mobile layout, offline freeze/reconnect, and server restart recovery.
- Lost HTTP acknowledgement after an accepted action: the browser applied the streamed result and did not replay the mutation.
- Responsive widths: 320, 375, 768, and 1440 pixels; no horizontal document overflow in the checked travel view.
- Checked-in systemd units and Apache proxy configuration syntax.

Desktop/mobile driving captures are in `output/playwright/backend-driving-desktop.png` and `output/playwright/backend-driving-mobile.png`.

## Limits

Journeys, scores, and tombstones remain in memory. Restarts create a new journey incarnation; replicas need sticky routing. The backend rewrite does not change game balance or exhaustively retest every randomized event branch. The full gameplay checklist remains available in `tests/WEB_E2E_CHECKLIST.md`.
