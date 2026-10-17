# The Asphalt Trail

It's 2028. Your house in Cape Coral, Florida has been declared uninsurable, so you're loading the family into the car and driving to Seattle, which is at least uphill from the sea. This is The Oregon Trail rebuilt for that trip: phosphor text, original ASCII scenes, numbered commands, and a car that fills up with increasingly questionable things.

It runs in the browser. The game rules are C# on .NET 8, the page is plain JavaScript and CSS, and nothing is fetched from third-party services: no fonts, images, or scripts.

## Playing

Click or tap a command, or use the keyboard: **1–9** picks a numbered action, **↑/↓** moves between them, and **Enter** activates whatever has focus. **?** opens the field guide and **Escape** closes it.

Pick **Plan your trip** on the title screen to start. The opening story runs five chapters, starting with **Notice of non-renewal**, the form letter from Sunshine State Mutual with its smiling ASCII sun: "UNINSURABLE AT ANY PREMIUM." and "HAVE A SUNNY DAY!" Use **Next chapter** to keep reading, or skip ahead to choosing your background.

You pick a background, one of four vehicles (each with its own silhouette and stats), and a party, then shop at the first travel center. The store shows a running receipt against your cash and cargo limits. On the road, the car and scenery animate while you're actually driving and stop when you park, rest, break down, lose the connection, or hit an event. The driving animation has its own pause button, separate from the CRT switch, and starts paused if your system asks for reduced motion. Pausing never hides the trip status. Live updates leave your focus and any half-typed input alone.

Make it to Seattle and the same sun comes back with one more line: "Sunshine State Mutual regrets to inform you your new ZIP code is also under review." Then the game counts up your Net Worth and Clout.

### RUG.RUN

Choose **Launch a crypto coin** from the travel menu, either at a stop or pulled over between stops. Name a fictional coin, pick a narrative and seed capital, and you get a 120-second market to work with. There are six hype campaigns. Each one takes time, costs road cash, and works less well every time you repeat it. You watch the candles, volume, liquidity, credibility, wallet suspicion, and market feed, and try to hit **PULL OUT / RUG IT** at the right moment. Withdrawing takes three market ticks, and the payout includes price impact and fees. Most launches lose money. If the listing expires you get a distressed sale, and if the pool collapses you get nothing.

Each launch costs one trail day when you come back to the road. Your founder history and notoriety last for the rest of the journey.

### DEAD AIR

Start a **YouTube travel channel** anywhere on the trail, or use **Pull over / YouTube travel channel** while driving. Name the channel, then spend **one trail day and $8** filming whatever happens and publish the idea you were handed. You can carry unfinished footage to another stop. Most ideas are dull, and only the rare excellent one has any chance of breaking out. Subscriber traffic, discovery views, new subscribers, and unsubscribes all vary from upload to upload, and a hidden recommendation score reacts to both kinds of audience.

Gear comes from **V&H — Victor & Horoshilov**: cameras, lights from $19 to $1,499, SD cards, backup storage, batteries, HDMI monitors, mount-specific lenses, ND filters, microphones, and supports. Orders are charged right away and arrive one trail day later, whether you spend that day driving or filming. You equip delivered cameras from your bag. Compatible accessories fit automatically, and only the best item in each category counts. Gear raises the ceiling on how many people can see a video. It never makes anyone want to watch it. Footage keeps whatever equipment it was shot on.

The in-game Partner Program opens at **500 subscribers and 10,000 lifetime views**. The game made those numbers up; don't take them as YouTube's actual requirements. After that, eligible uploads earn ad revenue, which you can move to road cash when you're owed at least **$10**. Every original upload has a **15%** chance of losing ads because of something in the background nobody noticed. Spending **one day re-editing** fixes it once: the reupload gets **half the original views**, rounded down, and subscribers aren't counted twice. Browsing, publishing, and getting back on the road don't cost trail days, but the family keeps eating while you film, wait for a delivery, or re-edit.

The studio tracks views by source, gear, recent uploads, churn, orders, ad income, withdrawals, and net profit after everything you've spent. The library keeps your last 60 uploads, and lifetime totals keep the rest. The 200 video ideas were written in 20 separate batches, are scored on the server, and some can only be filmed in certain places. The channel is meant to be a money pit with the occasional profitable career.

All of it, like everything else in the game, lives in memory and disappears when the service restarts.

## Running it

You need the .NET 8 SDK. There's no frontend build step and nothing to install from npm.

```bash
dotnet run --project src/OregonTrailDotNet.csproj --urls http://127.0.0.1:8080
```

Then open `http://127.0.0.1:8080/`.

## How it works

```text
Browser: semantic HTML + local road animation
       │ commands + versioned state stream (SSE)
       ▼
ASP.NET Core: HTTP API, admission limits, cached JSON
       │ bounded command mailbox per journey
       ▼
Independent journey workers → C# game rules / WolfCurses adapter
```

Every journey gets its own worker and execution context, so one slow game can't hold up the rest. Commands and clock ticks go through a bounded mailbox in order. Reads come from snapshots that were serialized ahead of time, and the browser gets changes over server-sent events instead of polling. A slow connection only ever holds the latest full snapshot. `/healthz` checks the host without creating a game.

`GET /api/game` sets an HttpOnly cookie and returns the current snapshot: a `journeyId`, a monotonic `revision`, the legal actions, and an explicit `driving` state. `GET /api/game/events` streams changes and heartbeats. To act, `POST /api/game/actions` with `{ actionId, expectedRevision, expectedJourneyId, text?, value? }`, using an action the server advertised. A stale revision or a journey from a previous host run gets HTTP 409 with the current state. Nothing is replayed automatically on reconnect.

The host admits 256 sessions by default and won't evict a live journey to make room for someone new. Idle sessions expire after 12 hours, and a simulation pauses 30 seconds after its last tab or request goes away. If you run more than one replica, you need sticky routing. The limits are under `GameHost` in configuration. Journeys, high scores, and tombstones are all in memory only.

In a local test with 16 fresh journeys starting at once, startup dropped from **17.4 seconds to 1.8 seconds**. That was measured on a development machine, so don't treat it as a capacity number. A healthy browser connection makes no repeating snapshot requests.

## Testing and publishing

```bash
./tools/build_web.sh
dotnet run --project tests/Backend/BackendTests.csproj
./tools/verify_web_rewrite.sh
dotnet publish src/OregonTrailDotNet.csproj -c Release -o ./publish
```

For the HTTP integration checks, start a throwaway host and run:

```bash
python3 tests/backend_integration.py http://127.0.0.1:8080
python3 tests/crypto_integration.py http://127.0.0.1:8080
python3 tests/creator_integration.py http://127.0.0.1:8080
python3 tests/food_sweep_integration.py http://127.0.0.1:8080
```

These cover concurrent journeys staying independent, command conflicts, stream reconnects, all four vehicle setups, stores, and starting and stopping the car. The backend harness covers bounded queues, slow subscribers, cancellation, idle clocks, and shutdown.

The backend harness also plays 9,000 deterministic crypto launches across three narratives, three stake sizes, and hold, hype, and timed-exit strategies, checking campaign timing, cash accounting, validation, and settlement. The crypto HTTP checks hit the real live stream, replay protection, campaign locks, payouts, and the trail-day cost.

The creator checks cover gear compatibility, gear only raising the ceiling, random audience sources, hidden recommendations, exactly-once payouts, half-view reuploads, filming locations, and 20,000 moderation samples. The balance harness runs 40,000 careers with phone-only, cheap-light, mirrorless, and longer-filming strategies. The target is that roughly 8% of frugal **12-upload phone careers** turn a profit, re-editing flagged videos and counting ad income minus production costs. Buying gear, filming longer, and dying on the road all change those odds. The video-idea sources and duplicate checks live in `tools/creator-content/`; run `python3 tools/creator-content/pool.py audit --complete` to audit the full pool.

The manual browser checklist is [tests/WEB_E2E_CHECKLIST.md](tests/WEB_E2E_CHECKLIST.md). Proxy and release setup is in [DEPLOYMENT.md](DEPLOYMENT.md).

## Where things are

- `src/Web/`: endpoints, journey workers, the engine adapter, and the browser contract
- `src/wwwroot/`: the browser client, animation, and styles
- `src/Window/`, `src/Module/`, `src/Entity/`, `src/Event/`: the game's state machine and rules
- `tests/Backend/`, `tests/backend_integration.py`: worker tests and real HTTP checks
- `sim/`: the balancing simulator
- `web/`: an earlier Bonsai prototype, kept for reference and not loaded
