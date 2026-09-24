# DEAD AIR verification

The travel-channel career can start at a stop or by pulling over during a drive. This verification uses disposable local journeys; no public deployment or external YouTube account is involved.

## Browser checks

Verified with the real ASP.NET host and Playwright:

- Channel creation, keyboard submission, and a persistent custom name.
- Random filming, a saved draft across studio visits, publication, and audience-source reports.
- Real trail-day costs, no added miles, and unchanged cash when browsing.
- A $19 light order, next-day delivery, and a ceiling captured before the delivery.
- A $749 mirrorless order, delivery, camera selection, enabled VH-E lenses and HDMI displays, and blocked VH-C lenses.
- An actual moderation incident: the 608-view upload was re-edited through the UI to 304 views, for one trail day. It retained the resolved incident and removed the repair action.
- Focus moves to channel, draft, published-video, and repaired-video headings. Shop category focus survives filtering. The catalog does not consume core numbered shortcuts.
- Studio, draft, shop, and library at 320px/390px and desktop widths, with no horizontal overflow. Controls meet the 44px touch-target minimum.
- Reduced motion and CRT effects off; the channel remains usable. The final browser console has no errors or warnings.

Screenshots: [onboarding](playwright/creator-onboarding-desktop.png), [mobile shop](playwright/creator-shop-mobile.png), [mobile reupload](playwright/creator-reupload-mobile.png), [desktop library](playwright/creator-library-desktop.png).

## Scope and limits

State follows the existing memory-only journey lifetime. The library retains 60 recent uploads and keeps lifetime totals. Monetization uses fictional game thresholds. Gear changes the audience ceiling; it cannot create demand. Profitability depends on filming and purchasing choices, so the balance target is measured against a declared 12-upload phone policy rather than guaranteed for every strategy. Trail supplies and survival remain additional costs outside the channel's ledger.

An independent code review found no substantive issues in the domain, trail-day integration, presentation, dispatch, or browser implementation.

## Final automated results

The user reduced the requested pool to 200 ideas. Twenty actual `gpt-6-luna` authors at `xhigh` supplied ten each. The complete audit passes with 120 weak, 60 middling, and 20 great ideas; 192 are generic and eight require the matching stop. IDs, normalized titles, and concept keys are unique. Every record matches its author submission. Full semantic review and the one lexical-overlap candidate are resolved in [the editorial review](../tools/creator-content/review.md).

`dotnet run --project tests/Backend/BackendTests.csproj --no-restore` passes creator rules, catalog checks, 40,000 simulated careers, existing crypto tests, bounded worker queues, cancellation, session isolation, lifecycle, and shutdown checks. Moderation occurred in 2,999 of 20,000 seeded uploads (14.99%).

Each balance cohort contains 10,000 independently seeded careers, re-edits flagged uploads, and reconciles every wallet transaction. Profit means earned ad revenue minus production and gear expenses, including unwithdrawn earnings:

| Policy | Net-positive careers | Mean channel net |
| --- | ---: | ---: |
| Phone, 12 original uploads | 764 / 10,000 (7.64%) | −$75.80 |
| $19 light, 12 uploads | 740 / 10,000 (7.40%) | −$94.09 |
| $749 mirrorless kit, 12 uploads | 352 / 10,000 (3.52%) | −$779.73 |
| Phone, 24 original uploads | 1,177 / 10,000 (11.77%) | −$149.93 |

The complete Release build passes with zero warnings and errors. `tools/build_web.sh`, `WEB_VERIFY_NO_RESTORE=1 tools/verify_web_rewrite.sh`, and `git diff --check` pass. The verifier checks semantic API shape, isolated browser journeys, stale-action rejection, and served creator assets.

The real HTTP creator, backend, and crypto suites pass against the final Release host. The creator fixture was corrected to subscribe and await the actual driving tick before opening the channel; the ordinary driving regression explicitly chooses the stop action. This avoids depending on the first button or arbitrary transition timing.
