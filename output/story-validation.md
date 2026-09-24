# Opening story flow — 2026-09-23

Removed the always-visible background-selection shortcut from the story. Chapters 1–4 now offer a prominent Next chapter action; chapter 5 offers Choose your background. Previous remains available. All authored prose is unchanged.

Verified in Chromium: all five chapters in sequence, no early background button or numeric-key bypass, Enter activation, previous/next navigation, final transition to background selection, and no horizontal overflow at 375px. Reviewed mobile chapter-one and desktop chapter-five screenshots in output/playwright. JavaScript syntax and git whitespace checks passed.

Deployed app.js, styles.css, and cache-busted index.html to the active release without restarting the service or resetting journeys. Public assets matched the checked local files byte for byte, and the five-chapter sequence passed again on the live site. Prior browser assets are backed up at /opt/oregon-trail-web/story-assets-before-20260923.

## Live recheck — 2026-09-24

Confirmed in a fresh Chromium session at https://wagenhoffer.dev/oregon/: **Plan your trip** opens chapter one, **Notice of non-renewal**, with the prose describing the insurance letter and the HOA clubhouse joke. This check initially mistook that prose for the actual insurer card. The browser adapter preserved the prose but omitted the ASCII sun, insurer notice, and Seattle callback from the terminal version. The screenshot of that earlier state is retained locally at `output/playwright/insurance-intro-live.png`.

The Release backend suite, creator content audits, browser asset syntax checks, and `WEB_VERIFY_NO_RESTORE=1 bash tools/verify_web_rewrite.sh` passed before committing the current version.

## Restored opening and ending — 2026-09-24

Restored the exact ASCII sun and three notice lines from `a5a9de4a` at the beginning of chapter one: `~ SUNSHINE STATE MUTUAL ~`, `* UNINSURABLE AT ANY PREMIUM. *`, and `"HAVE A SUNNY DAY!"`. The art is decorative; the notice is readable HTML. The Seattle arrival screen repeats the notice and sun and restores the original congratulations, insurer's warning that the new ZIP code is also under review, and invitation to tally the final score.

Compared all sun characters and all three notice lines against that commit. Web verification passed with no build warnings or errors. Chromium checks passed at desktop, 375px, and 320px widths, including Previous/Next chapter navigation and the transition to background selection. The notice appears only in chapter one and at the Seattle arrival, not on the final score screen.

A temporary copy of the live-rule simulator completed seed 40001 with four survivors and a score of 3,958. Its actual `GameWin` and `FinalPoints` snapshots were used as browser preview fixtures to verify the callback, responsive ending, and score transition. Screenshots are retained locally in `output/playwright/insurance-notice-*` and `output/playwright/insurance-ending-*`.

Deployed the combined update and verified public health, every published asset, the browser's versioned asset URLs, secure cookies, and live stream state/heartbeat delivery. A fresh live Chromium session confirmed the restored opening with zero console errors or warnings. Release details are recorded in `output/deployment-insurance-intro.json`.
