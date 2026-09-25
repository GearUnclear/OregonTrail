# Web end-to-end checklist

`tools/verify_web_rewrite.sh` provides build, contract, stale-revision, and asset smoke tests. Run `dotnet run --project tests/Backend/BackendTests.csproj` for worker backpressure, cancellation, idle lifecycle, and capacity checks. Run `python3 tests/backend_integration.py BASE_URL` against a disposable host for concurrent session isolation, streamed state, reconnect, all four vehicle setups, and driving transitions. The randomized WolfCurses journey does not yet expose a deterministic test scenario, so the complete gameplay path remains a manual or Playwright-assisted release check.

## Preparation

1. Check the browser assets with `./tools/build_web.sh`.
2. Run `./tools/verify_web_rewrite.sh`.
3. Start a fresh host on loopback and open its root page in a new browser context. Each browser context should receive its own `asphalt_trail_session` cookie and independent journey.
4. Open a second context, mutate only the first journey, and confirm the second remains on its original revision and screen.
5. In browser automation, select controls by accessible role and name. Avoid CSS classes and terminal copy; those are presentation details.

## Journey setup and store

- Confirm the initial response and page expose a semantic `setup` screen, a single page heading, and named buttons.
- Start a journey, choose each profession and vehicle at least once across test runs, and verify the HUD and ASCII art reflect the selected vehicle. All four vehicle cards must show distinct silhouettes; hover and keyboard focus animate only the active preview.
- Enter party names using the visible labelled input. Verify keyboard Enter and the submit button have the same result, validation is announced, and focus moves to the next meaningful heading/control.
- Confirm the names and choose a starting month.
- On the initial `store` screen, verify cash, pending total, cargo usage, unit prices, quantities, and line totals update without replacing the whole gameplay region.
- Increase and then decrease a non-first store row. Confirm only that row changes, its minimum/maximum disables the appropriate control, and balance/cargo limits cannot be exceeded.
- Attempt to leave with an invalid loadout and verify the error is visible and announced. Correct the loadout, leave, and verify purchases appear in inventory.

## Travel and timed state

- Verify the `travel` view shows date, location, health, pace, rations, mileage, next location, party, and inventory as semantic responsive elements.
- Open supplies and map/status views, acknowledge them, change pace and rations, and choose a rest duration. Confirm numeric limits and validation.
- Continue travelling. Confirm server-pushed timed progress changes while focus and any in-progress input remain stable; unchanged snapshots must not replace DOM nodes or reset controls.
- Check that driving animates the selected vehicle's wheels and road markings, with exhaust on combustion vehicles and a battery indicator on the EV. Parked, resting, and disabled vehicles must not show spinning wheels or moving road markings.
- Turn CRT off while driving: car and road motion must continue. Check the separate Pause/Play animation control, its saved preference, reduced-motion defaults, and explicit Play override at 320px width.
- Stop travelling and ensure the next travel menu reflects the updated date, mileage, resources, and available location actions.
- Exercise a river choice when reached and verify river facts and legal actions are presented as a `river` view.
- Exercise the food sweep from the roadside menu: it opens directly into a timing track with no text field. Tap Grab or press Space in the striped zone; check early/late misses, automatic tray expiry, inline feedback, the 100 lb cap, and one result screen. Completing and collecting the haul spends exactly one day and awards food once.
- At 390px and 320px widths, keep the marker and Grab button visible together; test touch taps, Space, held-key suppression, reduced motion, and reconnect. No horizontal overflow or intermediate hit/miss dialogs.
- Exercise DoorDash when available. Check timers/progress, action enablement, expiry, results, and keyboard operation.

## Events and end state

- Continue until a random event appears. Verify the `event` title/description is announced once, its advertised actions work, and returning to travel updates affected party/inventory fields.
- Exercise both a choice event and a simple acknowledgement event.
- Complete or lose a journey in a dedicated long-running test. Verify the `game-over` summary, score data, epitaph flow when applicable, and restart action.
- Until a seeded test host exists, record the random seed/run transcript and the modes actually covered with the release evidence.

## Revision and failure behavior

- Delay a GET response in the browser/network harness, submit an action, then release the older GET. The displayed revision and screen must not move backwards.
- Rapidly activate one action. Only one request should be in flight and the mutation should occur once.
- Replay an action with an old `expectedRevision` or `expectedJourneyId`; verify the client consumes the `409 stale-revision` state and recovers without losing focus or typed text unnecessarily.
- Simulate an offline request and a `400 invalid-value`; verify errors are announced and retry/recovery is possible.

- Confirm the healthy browser opens one event stream and makes no recurring GET requests while a menu remains unchanged.
- Interrupt the stream and verify reconnect/fallback retrieves current state without automatically replaying an action. Stop the host and restart it: the new journey incarnation must replace an old higher revision.
- Hide the last game tab, wait through the 30-second grace period, and verify simulation pauses; return to the tab and confirm it reconnects. Multiple tabs in the same browser must share a journey and remain synchronized.
- On the road, verify the visible status, selected vehicle, destination, miles remaining, pace, and weather. Road markings, wheels, and distant scenery move. Parked, event, offline, and reduced-motion views must freeze the art while preserving status text.

## Accessibility and responsive passes

- Read all five opening chapters; verify Previous/Next bounds and that story navigation preserves the current chapter across live updates.
- Check numbered commands, arrow navigation, input typing without shortcut interference, the **?** field guide, Escape, and focus restoration.
- Toggle CRT effects, reload, and verify the preference persists and ASCII animation stops while effects are off. Enable reduced motion while a preview or driving scene is active and confirm the art freezes immediately. Verify ASCII is hidden from assistive technology and prose remains ordinary reflowing text.
- Verify roadside dialogue names its speaker and retains the quote; trade and toll choices explain what will be exchanged or charged.
- In the epitaph flow, submit multiple words, verify the visible draft, submit an empty line to finish, and check that the confirmation's Yes/No question matches its action.
- Complete setup, store, and one travel/event cycle using only the keyboard.
- Check visible labels, logical heading order, focus visibility, screen-change focus management, disabled states, status announcements, and non-colour error cues.
- Run an accessibility scanner when one is available; no scanner dependency is committed to this repository.
- Test 320 px, 375 px, 768 px, and desktop widths. There must be no page or gameplay-region horizontal scrolling, clipped actions, or controls smaller than 44 by 44 CSS pixels.
- Test 200% browser zoom and reduced-motion mode.

## Future automation seam

Add a test-only dependency-injected random source and scenario builder before committing a deterministic Playwright suite. It should be available only in the test host, permit direct setup/store/travel/event coverage, and never expose state-forcing endpoints in production.
# RUG.RUN regression checks

- Open **Launch a crypto coin** from the travel menu at a stop and while pulled over between stops. Closing the unlaunched desk must cost no money or days.
- Choose a name, narrative and stake, then launch. Verify the seed and $12 fee leave the road wallet immediately, while driving stays stopped.
- Run a hype campaign. Confirm cost is charged once, other campaigns are disabled until it lands, and repeated campaigns observe cooldowns. The graph and volume must keep updating.
- On a phone, launch should show the graph and exit button. Scroll down to the campaigns; the exit must stay visible. Check keyboard `1` and focus preservation across ticks.
- Turn CRT off, then use the operating system's reduced-motion preference. Effects must stop while market data continues updating.
- Pull out and watch the three-tick settlement. Repeated clicks/replayed requests cannot pay twice. Verify the receipt against the road wallet.
- Let a launch expire or collapse. It must settle without further input. Leaving a finished launch consumes exactly one trail day, and reopening preserves the founder history within that journey.

## DEAD AIR regression checks

- Open **YouTube travel channel** at a stop and **Pull over / YouTube travel channel** while driving. The latter must park immediately; browsing and leaving spend no trail days. Start with a custom name, reject invalid names, and preserve unfinished text across renders.
- Film with the phone. Charge $8 and one trail day, consume the family's normal supplies, and add no miles. The randomly assigned title appears after filming. Publish exactly once, or discard without a refund. Leaving/reopening and reloading must preserve drafts, gear, accounts, subscribers, and uploads.
- Order a cheap light, then film. Cash leaves when ordered; delivery occurs after one trail day, and the new light raises the next film's ceiling. Existing footage retains its original cap. Duplicate commands cannot charge twice.
- Check all 34 V&H products and every category filter. Prices, specs, satirical descriptions, affordability, parcel status, and ownership must be readable. Filters retain focus. Gear buttons must not consume the numbered shortcuts for core studio actions.
- Order, receive, and equip a VH-E camera. Matching lenses and HDMI monitors unlock; VH-C lenses remain blocked. Switch back to the phone and verify incompatible supporting equipment no longer contributes to the ceiling. The strongest accessory per category fits automatically.
- Inspect subscriber/discovery views, subscriber gains/losses, net costs, unpaid ads and withdrawals. The library chart has accessible text; the hidden algorithm and idea potential never appear in API payloads or client assets. All monetization thresholds are described as fictional game rules.
- On a demonetized upload, inspect the named incident and exact reupload preview. Re-edit once: one day, exactly floor(original views /2), no repeated subscriber gains, and no second repair control. The result receives focus and the incident remains as a resolved history entry.
- Check channel creation, draft creation, publication, and repair all move focus to their meaningful result heading. Test keyboard shortcuts, 320px/390px/desktop layouts, CRT off, reduced motion, and keyboard focus in the shop. No horizontal gameplay overflow or tiny touch controls.
- Verify the monetized earning and transfer paths with the seeded domain harness; the ordinary browser journey should not be expected to turn profitable. Use `tests/creator_integration.py` for real HTTP entry, day costs, cash, SSE, replay protection, hidden fields, and session isolation.
