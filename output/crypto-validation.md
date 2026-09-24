# RUG.RUN validation

Verified locally on 2026-09-24 against the ASP.NET host at `127.0.0.1:18765`.

## Automated checks

- `dotnet build src/OregonTrailDotNet.csproj --no-restore --nologo`: passed, no warnings or errors.
- `./tools/build_web.sh`: passed, including syntax checks for `crypto.js`.
- `dotnet run --project tests/Backend/BackendTests.csproj --no-restore`: passed. Covers invalid configuration, insufficient cash, delayed campaigns, campaign locks/cooldowns, three-tick withdrawals, exactly-once settlement, bounded histories, reproducible markets, and the existing worker lifecycle checks.
- `WEB_VERIFY_NO_RESTORE=1 ./tools/verify_web_rewrite.sh`: passed, including a Release build and HTTP revision/isolation checks.
- `python3 tests/crypto_integration.py http://127.0.0.1:18765`: passed. Uses actual HTTP/SSE to check setup, naming validation, cash debits, launch replay rejection, live candles, campaign locks, settlement, one-day return cost, and journey isolation.

## Seeded balancing cohorts

Each policy ran 3,000 independent launches, cycling through all three narratives and all three stakes. These are fixed simulation samples, not a guarantee of any particular player's odds.

| Policy | Losses | Wins | Mean net |
| --- | ---: | ---: | ---: |
| Hold until expiry/collapse, no campaigns | 3,000 (100%) | 0 | -$93.77 |
| Hype through 70 seconds, then request exit | 2,880 (96.0%) | 117 | -$98.72 |
| Use campaigns; exit at a 25% quoted profit or 42 seconds | 2,689 (89.6%) | 307 | -$75.15 |

Remaining runs broke even. All 9,000 launches reconciled the wallet against their receipts and remained within finite, nonnegative market bounds.

## Browser checks

Used the Playwright CLI through the actual new-game, shopping, travel, and exchange flows.

- Custom name and stake selection persisted through launch.
- At 390 × 844, document width remained 390 pixels, with no horizontal overflow.
- Live price changed while the pull-out button retained keyboard focus.
- Canvas pixels confirmed active spark rendering.
- CRT off cleared the particle layer while new server price observations continued.
- `prefers-reduced-motion: reduce` disabled the live animation and cleared effects.
- Launching on mobile focused the dashboard: heading at 24px, chart at 255px, pull-out button at 635px in an 844px viewport.
- While viewing lower campaigns, the sticky pull-out button remained between 78px and 132px in the viewport.
- Keyboard `1` requested withdrawal; final settlement focused the receipt heading.
- Returning advanced March 1 to March 2 and consumed the party's daily food. Reopening preserved the coin receipt, cumulative net, and notoriety.
- No JavaScript errors. One diagnostic canvas readback warning came from the verification script's repeated `getImageData` calls; gameplay does not perform those readbacks.

Screenshots are in `output/playwright/`: `crypto-lobby-desktop.png`, `crypto-lobby-mobile.png`, `crypto-live-desktop.png`, `crypto-live-mobile.png`, `crypto-hype-burst.png`, `crypto-result-desktop.png`, and `crypto-result-mobile.png`.
