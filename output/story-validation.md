# Opening story flow — 2026-09-23

Removed the always-visible background-selection shortcut from the story. Chapters 1–4 now offer a prominent Next chapter action; chapter 5 offers Choose your background. Previous remains available. All authored prose is unchanged.

Verified in Chromium: all five chapters in sequence, no early background button or numeric-key bypass, Enter activation, previous/next navigation, final transition to background selection, and no horizontal overflow at 375px. Reviewed mobile chapter-one and desktop chapter-five screenshots in output/playwright. JavaScript syntax and git whitespace checks passed.

Deployed app.js, styles.css, and cache-busted index.html to the active release without restarting the service or resetting journeys. Public assets matched the checked local files byte for byte, and the five-chapter sequence passed again on the live site. Prior browser assets are backed up at /opt/oregon-trail-web/story-assets-before-20260923.

## Live recheck — 2026-09-24

Confirmed in a fresh Chromium session at https://wagenhoffer.dev/oregon/: **Plan your trip** opens chapter one, **Notice of non-renewal**, with the original insurance letter, smiling cartoon sun, and HOA clubhouse joke. The story remains after the title screen and before background selection. No narrative was removed from `GameIntro.cs`; its changes expose the existing prose to the browser. The screenshot is retained locally at `output/playwright/insurance-intro-live.png`.

The Release backend suite, creator content audits, browser asset syntax checks, and `WEB_VERIFY_NO_RESTORE=1 bash tools/verify_web_rewrite.sh` passed before committing the current version.
