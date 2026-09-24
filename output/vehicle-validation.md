# Vehicle artwork follow-up — 2026-09-23

The public `/oregon/scenes.js` and installed release `20260923T205823Z-terminal` still contain one shared `car` drawing. The newer distinct vehicle art and live-driving backend had not been deployed.

Fixed an additional setup-preview bug: HUD vehicle identity now comes from `NewGameInfo.VehiclePick` during setup, rather than waiting for the actual journey vehicle to be created. Added an integration assertion after vehicle selection and versioned all three browser scripts to fetch the matching release.

Validated the complete release in `/tmp/asphalt-vehicle-release`:

- Release publish, JavaScript syntax, worker harness, web contract verification, and full HTTP integration all passed.
- Chromium displayed four different choice-card drawings at 1440px and 375px, with no mobile horizontal overflow.
- Selecting the EV immediately changed the setup preview to the EV.
- Four separate full journeys displayed the correct, distinct parked vehicle art.
- EV driving frames changed while moving and stopped changing when parked.

Screenshots: `playwright/cars-four-choices-desktop.png`, `playwright/cars-four-choices-mobile.png`, `playwright/car-*-parked.png`, and `playwright/car-ev-driving-verified.png`.

The user explicitly approved deployment after the initial automatic approval block. Deployed `/opt/oregon-trail-web/releases/20260923T224300Z-vehicles` with the matching Apache proxy configuration. The prior release `20260923T205823Z-terminal` is retained for rollback. Local and public health checks passed. All five public HTML/JS/CSS assets matched the tested release byte for byte. Chromium verified all four distinct choice-card drawings and the selected EV setup preview on `https://wagenhoffer.dev/oregon/`. Public SSE delivered an initial state and heartbeat, and the session cookie was Secure. The 17-second SSE capture ended with the expected curl timeout. Live screenshot: `playwright/live-distinct-vehicles.png`.
