# The Asphalt Trail

A road-trip survival game from Cape Coral to Seattle in 2028, presented as a roadside terminal: phosphor text, original ASCII scenes, numbered commands, and the increasingly questionable contents of your car.

## Architecture

```text
Browser: semantic HTML + local road animation
       │ commands + versioned state stream (SSE)
       ▼
ASP.NET Core: HTTP API, admission limits, cached JSON
       │ bounded command mailbox per journey
       ▼
Independent journey workers → C# game rules / WolfCurses adapter
```

Each journey has its own asynchronous worker and execution context. One slow journey cannot lock every other game. Commands and clock pulses run in order through bounded mailboxes; HTTP reads use immutable, pre-serialized snapshots. Clients receive changes through server-sent events instead of repeatedly polling unchanged screens. Slow connections retain only the latest full snapshot. A health endpoint checks the host without creating a game.

`GET /api/game` establishes an HttpOnly browser cookie and returns the current snapshot, including a `journeyId`, monotonic `revision`, legal actions, and explicit `driving` state. `GET /api/game/events` streams state changes and heartbeats. Submit an advertised action to `POST /api/game/actions` with `{ actionId, expectedRevision, expectedJourneyId, text?, value? }`. Stale revisions or previous host incarnations return HTTP 409 with the current state. Commands are never automatically replayed on reconnect.

The browser animates the selected vehicle, wheels, road markings, and passing scenery while the server reports driving. A visible “On the road” panel shows the destination, remaining distance, pace, and weather. Motion stops when parked, resting, disabled, disconnected, or interrupted by an event. The CRT switch and reduced-motion preference stop animation while keeping driving information visible. Live updates preserve focus and unfinished input.

## Play

Click or tap commands, use **1–9** for numbered actions, **↑/↓** to move between them, and **Enter** to activate a focused control or submit a form. **?** opens the field guide; **Escape** closes it. The opening story has five chapters. All four vehicles have distinct silhouettes and comparison facts. Travel centers include quantity controls and a live receipt with cash and cargo limits. No fonts, images, or scripts are fetched from third-party services.

Select **Plan your trip** on the title screen to read the opening story. Chapter one, **Notice of non-renewal**, opens with the original Sunshine State Mutual notice and ASCII sun: “UNINSURABLE AT ANY PREMIUM.” and “HAVE A SUNNY DAY!” The story about the letter and the HOA clubhouse joke follows. Use **Next chapter** to read the remaining chapters before choosing your background.

Reaching Seattle brings back the same notice and sun with the original callback: “Sunshine State Mutual regrets to inform you your new ZIP code is also under review.” Continue to tally your final Net Worth and Clout score.

**RUG.RUN:** Choose **Launch a crypto coin** from the travel menu while parked at a stop or between stops. Name a fictional coin, choose a narrative and seed capital, then run six different hype campaigns during a 120-second market. Campaigns take time, cost road cash, and lose impact with repetition. Live candles, volume, liquidity, credibility, wallet suspicion, and the market feed help time the **PULL OUT / RUG IT** button. Withdrawing takes three market ticks; the final cash payout includes price impact and fees. Most launches lose money. Letting the listing expire triggers a distressed sale; a collapsed pool returns nothing.

Each launched coin costs one trail day when you return. Your founder history and increasing notoriety last for the current journey. The exchange's sparks, pump bursts, and crash effects respect the CRT switch and reduced-motion preference. Like the rest of the journey, exchange records are held in memory, not saved across service restarts.

**DEAD AIR:** Start a **YouTube travel channel** anywhere along the trail, or use **Pull over / YouTube travel channel** while driving. Name the channel, film whatever happens for **one trail day and $8**, and publish your randomly assigned idea. You can carry unfinished footage to another stop. Most ideas are dull; only the rare excellent idea has a chance to break out. Subscriber traffic, discovery views, new subscribers, and departures vary on every upload. A hidden recommendation score reacts to both audience sources.

Order cameras and supporting equipment from **V&H — Victor & Horoshilov**: lights from $19 to $1,499, SD cards, backup storage, batteries, HDMI monitors, mount-specific lenses, ND filters, microphones, and supports. Orders debit road cash immediately and arrive after one trail day, whether spent traveling or filming. Equip delivered cameras in your bag. Compatible supporting gear fits automatically; only the strongest item in each category counts. Equipment raises the maximum possible audience, never the underlying demand for a video. Existing footage keeps the equipment it was filmed on.

The fictional in-game Partner Program unlocks at **500 subscribers and 10,000 lifetime views**. Eligible uploads earn variable ad revenue; transfer it to road cash once the unpaid balance reaches **$10**. These are game rules, not real YouTube eligibility requirements. Every original upload has a **15%** chance of losing ads to an unnoticed background incident. Spend **one day re-editing** to restore eligibility: the reupload gets **half the original views**, rounded down, without counting subscriber gains twice. An upload can be repaired once. Browsing, publishing, and returning to the road are free in trail days; the family still eats while you film, wait for deliveries, or re-edit.

The studio tracks views by source, equipment, recent upload history, subscriber churn, delivery orders, ad income, withdrawals, and net profit after all production and gear spending. Its library retains the most recent 60 uploads; lifetime totals retain older work. The 200-idea pool is scored on the server, authored in 20 independent batches, and includes enforced filming-location locks. The channel is deliberately a money trap with rare profitable careers. It follows the same memory-only journey lifetime as the rest of the game.

## Run

Requires the .NET 8 SDK. Browser assets are plain JavaScript and CSS; no frontend compiler or package installation is required.

```bash
dotnet run --project src/OregonTrailDotNet.csproj --urls http://127.0.0.1:8080
```

Open `http://127.0.0.1:8080/`.

## Verify and publish

```bash
./tools/build_web.sh
dotnet run --project tests/Backend/BackendTests.csproj
./tools/verify_web_rewrite.sh
dotnet publish src/OregonTrailDotNet.csproj -c Release -o ./publish
```

For HTTP integration checks, start a disposable host and run:

```bash
python3 tests/backend_integration.py http://127.0.0.1:8080
python3 tests/crypto_integration.py http://127.0.0.1:8080
python3 tests/creator_integration.py http://127.0.0.1:8080
```

These checks cover independent concurrent journeys, command conflicts, stream reconnects, all four vehicle setups, stores, and driving start/stop. The worker checks cover bounded queues, slow subscribers, cancellation, idle clocks, and shutdown. The browser checklist is in [tests/WEB_E2E_CHECKLIST.md](tests/WEB_E2E_CHECKLIST.md). See [DEPLOYMENT.md](DEPLOYMENT.md) for proxy and release configuration.

The backend harness also runs 9,000 deterministic crypto launches across three narratives, three stakes, and hold/hype/timed-exit policies, and checks campaign timing, cash accounting, validation, and settlement. Crypto HTTP checks exercise the real live stream, replay protection, campaign locks, payouts, and the trail-day cost.

Creator checks cover gear compatibility, ceiling-only effects, random audience sources, hidden recommendations, exact-once payouts, half-view reuploads, geography, and 20,000 moderation samples. The balance harness measures 40,000 careers across phone, cheap-light, mirrorless, and longer filming policies. The approximately 8% profitability target is defined for a frugal **12-upload phone career**, re-editing flagged videos and counting earned ads minus production costs. Gear spending, longer careers, and road survival change the odds. Content provenance and collision auditing live in `tools/creator-content/`; run `python3 tools/creator-content/pool.py audit --complete` to check the complete pool.

In a local comparison using 16 concurrent fresh journeys, initialization fell from **17.4 seconds to 1.8 seconds**. This is a development-host measurement, not a production capacity guarantee. Healthy browser connections make no recurring snapshot requests.

## Project layout

- `src/Web/` — endpoints, bounded journey workers, engine adapter, semantic presentation contract
- `src/wwwroot/` — browser transport, interface, local animation, styles
- `src/Window/`, `src/Module/`, `src/Entity/`, `src/Event/` — game state machine and rules
- `tests/Backend/`, `tests/backend_integration.py` — worker and real HTTP checks
- `sim/` — balancing simulator
- `web/` — earlier Bonsai reference prototype; not loaded

Journeys, high scores, and tombstones remain in memory and disappear when the service restarts. The host admits up to 256 sessions by default and never evicts a live journey to admit another player. Inactive sessions expire after 12 hours. Simulations pause 30 seconds after their last connected tab or request. Horizontal replicas require sticky routing. Limits are configurable under `GameHost`.
