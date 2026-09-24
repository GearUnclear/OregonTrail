# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A browser-based Oregon Trail clone. The game rules and screen state machine are C# on **.NET 8**, still backed by the `WolfCurses` NuGet package. The ASP.NET Core host in `src/Web/` runs independent journey workers and publishes versioned JSON snapshots over HTTP and server-sent events; the browser UI is plain JavaScript and CSS in `src/wwwroot/`. The earlier Bonsai prototype remains in `web/` as reference and is not loaded.

## Build & Run

```bash
dotnet run --project src/OregonTrailDotNet.csproj
```

`src/wwwroot/app.js`, `bridge.js`, `styles.css`, and `index.html` ship directly with the .NET publish output. Run `./tools/verify_web_rewrite.sh` before publishing; it checks the typed API, revision rules, and browser assets. The randomized full journey is covered by the manual browser checklist in `tests/WEB_E2E_CHECKLIST.md` until a deterministic fixture exists.

## Architecture

The whole game is a hierarchical state machine driven by the WolfCurses engine. The four layers below are the key to navigating the code.

### 1. Journey simulation — `GameSimulationApp` (`src/GameSimulationApp.cs`)
Extends `WolfCurses.SimulationApp`; accessed by the rules through an `AsyncLocal` execution context via `GameSimulationApp.Instance`. Each journey worker owns one simulation; no process-wide game singleton or engine lock remains. Created only through the static `Create()` factory (which throws `InvalidOperationException` if `Instance` is already non-null) and destroyed only by `OnPreDestroy()` setting `Instance = null`. It owns:
- The game **Modules** (see layer 4) and the active **Vehicle** entity.
- `AllowedWindows` (~lines 84–99) — the whitelist of top-level `Window` types the `WindowManager` may instantiate: `Travel`, `MainMenu`, `RandomEvent`, `Graveyard`, `GameOver`.
- `MAXPLAYERS = 4` — caps party size and how many names the new-game flow collects.

**Module lifetime is split (load-bearing):**
- `Scoring` and `Tombstone` are constructed **once** in `OnPostCreate()` and **survive `Restart()`** — they are only cleared via the main-menu Options screens.
- `Time`, `EventDirector`, `Trail`, and the `Vehicle` are rebuilt **every** `Restart()` (constructed in that order; `Time` must tick first). `Restart()` then adds `Travel` (bottom/always-present) and pushes `MainMenu` on top.

**Lifecycle chain:** `OnPostCreate` (persistent modules) → `OnFirstTick` (calls `Restart`) → `Restart` (per-game state + window stack) → the WolfCurses render lifecycle → `OnPreDestroy` (teardown).

`Program.cs` configures hosting, compression, trusted proxy headers, and configurable capacity. `Web/GameEndpoints.cs` owns `GET /api/game`, `GET /api/game/events`, `POST /api/game/actions`, and `/healthz`. `GameSession` owns bounded session admission and expiration; `JourneyWorker` serializes commands and coalesced pulses per journey through a bounded channel. `GameEngine` isolates the legacy rules; cached `GamePublication` objects serve reads and bounded SSE subscriptions. The browser contract is domain data, input metadata, and advertised actions; it does not expose character commands or rendered WolfCurses output.

### Web contract — `src/Web/`

`GameSnapshotDto` contains a `JourneyId`, explicit `Driving` state, a monotonic `Revision`, `Running`, shared `Hud`, `Party`, `Inventory`, `Progress`, nullable `Store` and `Crypto`, and a semantic `Screen` object. `Screen.Kind` selects the browser presentation and is one of `setup`, `travel`, `dialog`, `choice`, `store`, `status`, `river`, `activity`, `crypto`, `event`, or `game-over`. The progress payload includes route stops and the HUD includes food use and vehicle trouble. Choice actions can include comparison facts. Each screen also carries an `Id`, title/description, optional `InputSpecDto`, and its currently legal `GameActionDto` list.

RUG.RUN's market rules are isolated in `Module/Crypto/CryptoExchange.cs`, with injected randomness and wallet operations. `Window/Travel/Crypto/CryptoDesk.cs` connects it to the journey; `Web/CryptoPresentation.cs` copies immutable market snapshots and advertises `crypto.*` actions. The browser's `wwwroot/crypto.js` draws the graph and effects locally. Market ticks never advance trail days; leaving a completed launch advances one day exactly once. `TravelInfo.CryptoCareer` retains founder history within a journey. Keep payouts server-owned, debit costs before campaigns start, and settle each receipt only once. Run the seeded cohorts in the backend harness when changing balance.

DEAD AIR adds nullable `GameSnapshotDto.Creator` and the `creator` screen kind. `Module/Creator/CreatorChannel.cs` owns an injected-RNG/wallet career in `TravelInfo.CreatorCareer`; `CreatorCatalog.cs` owns the fictional V&H equipment catalog and an embedded `video-ideas.json` authored by 20 Luna/xhigh workers. `CreatorDesk` applies one real trail day after successful filming, re-editing, or waiting for orders, while parked. `creator.open` is available both in `TravelMenu` and during `ContinueOnTrail`; the latter pulls over. `Web/CreatorPresentation.cs` explicitly projects safe data—never serialize the career or draft idea directly, since algorithm and potential scores are hidden. Equipment only clamps audience demand; it never boosts demand, conversion, or recommendation rolls. Purchases are charged before next-day delivery; only compatible gear contributes, strongest per category. Original uploads independently roll 15% limited ads; a single re-edit earns exactly half the original views, with no duplicate subscribers. `wwwroot/creator.js`/`creator.css` render the studio, shop, and library. Test the declared 12-upload net-profit target with the backend cohort when changing balance; preserve the 200-idea/20-author catalog and geo locks.

Clients may only submit an advertised action to `POST /api/game/actions` as `{ actionId, expectedRevision, expectedJourneyId, text?, value? }`. The response always contains `{ accepted, errorCode, message, state }`: accepted actions return 200, stale revisions return 409/`stale-revision`, and illegal actions or values return 400. Store quantity changes are row-specific and absolute values should use the row's `SetActionId`; never recreate legacy left/right or raw-input endpoints.

When adding a game interaction, update both sides of this contract: publish a stable semantic action ID and structured state in the server adapter, then handle the relevant screen/action in `src/wwwroot/app.js`. Do not parse display copy to recover state or action values. The client must ignore older revisions within the same journey incarnation, accept a new `journeyId` after restart, and keep at most one mutation in flight. SSE publishes full snapshots only when the semantic state changes, with 15-second heartbeats and native reconnect; capped polling is a fallback. The client uses `driving.isDriving` for road motion, never a parsed screen ID. Keep animation local and respect connection, visibility, CRT, and reduced-motion state.

> `OnTick(true)` is the **OS/system tick**, NOT a game turn. A game turn is `GameSimulationApp.TakeTurn(bool skipDay)` (advances `TotalTurns` and calls `Time.TickTime`). Don't conflate them.

### 2. Windows — `Window<TCommands, TData>` (`src/Window/`)
Top-level menu screens, kept on a `WindowManager` stack (the engine ticks/renders the top one). The five Windows and their paired data objects:
- `Travel` / `TravelInfo` — the always-present base window (`src/Window/Travel/`).
- `MainMenu` / `NewGameInfo` — new-game setup chain (`src/Window/MainMenu/`).
- `RandomEvent` / `RandomEventInfo` — event display; has **no menu**, subscribes to `EventDirector.OnEventTriggered`.
- `Graveyard` / `TombstoneInfo` — death tombstones / epitaph flow.
- `GameOver` / `GameOverInfo` — win/lose tabulation (`GameOver.cs` plus `GameWin.cs`).

A Window builds its menu in `OnWindowPostCreate`/`OnFormChange` via `AddCommand(handlerMethod, TCommands.Value)`. `TCommands` is a **1-based** enum; each value's `[Description(...)]` is the on-screen label and the int is what the player types (display number comes from the enum value, not `AddCommand` order). `TData` is a `WindowData` subclass owned by the Window and shared with all its Forms. Push a window with `GameSimulationApp.Instance.WindowManager.Add(typeof(SomeWindow))`; pop self with `RemoveWindowNextTick()`. **`Travel` rebuilds its whole menu every `OnFormChange`** (via `UpdateLocation()`/`ClearCommands()`) because legal commands depend on `LocationStatus` — don't `AddCommand`-once for it.

### 3. Forms — `Form<TData>` / `InputForm<TData>` (sub-states within a Window)
The actual interactive screens (store, river crossing, hunting, rest, trade, name entry, profession select…), e.g. `src/Window/Travel/Store/Store.cs`, `src/Window/Travel/RiverCrossing/`. Each Form is tagged `[ParentWindow(typeof(SomeWindow))]` and its `TData` generic **must match** the parent Window's. It shares the parent's `TData` via the inherited `UserData` property (Forms never construct `TData`). Transitions: `SetForm(typeof(NextForm))` replaces the current form (**there is no form stack — "back" is hand-wired** via `ClearForm()` or an explicit `SetForm`); `ClearForm()` returns to the bare Window menu.
- Plain `Form`: override `OnRenderForm()` for text; for free-text/numeric input you **must** override `InputFillsBuffer => true` (default false) or `OnInputBufferReturned(string)` never fires.
- `InputForm` (yes/no/acknowledge dialogs): override `OnDialogPrompt()` and `OnDialogResponse(DialogResponse)`.

### 4. Modules — `WolfCurses.Module.Module` (`src/Module/`)
- **Time** (`src/Module/Time/`) — master day/month/year clock; start **2028 / March / day 1**; fixed **30-day months** (`Date.NumberOfDaysInMonth = 30`), **no leap years**. `TakeTurn(skipDay=false)` → `TimeModule.TickTime` → `OnTickDay` → `Trail.OnTick`. Nothing on the trail moves except via the time tick. `skipDay=true` ticks/fires events without consuming a calendar day or incrementing `TotalTurns`.
- **Trail** (`src/Module/Trail/`) — `TrailRegistry.cs` declares each trail as a `Location[]` (ordered `Settlement`/`RiverCrossing`/`Landmark`/`ForkInRoad`/`TollRoad`, each with a `Climate`) wrapped in `new Trail(locations, lengthMin, lengthMax)`. Segment distances are **randomized per game** in the band (32–164 for Oregon). `TrailModule` hardcodes `Trail = TrailRegistry.AsphaltTrail` in its ctor and tracks `LocationIndex`/`DistanceToNextLocation`. `NextLocation == null` signals end of game.
- **Director** (`src/Module/Director/`) — the event engine (see below).
- **Scoring** (`src/Module/Scoring/`) — a ranking **container only** (`List<Highscore>`, `TopTen`, `Add`/`Reset`), seeded from hardcoded `DefaultTopTen`. **It does NOT compute the score** (that's `FinalPoints.cs`, below). **Not persisted to disk** — `Destroy`/`Reset` are `// TODO: Save/Load … as JSON` stubs; player scores re-seed from defaults every launch.
- **Tombstone** (`src/Module/Tombstone/`) — in-memory `Dictionary<int, Tombstone>` keyed by `Vehicle.Odometer` mile marker (one grave max per marker). **NOT persisted to disk** despite class docstrings — the ctor has `// TODO: Need to code JSON saving and loading`. Graves survive a `Restart()` within one process run but are **lost on process exit**. Rediscovered during travel in `src/Window/Travel/Command/ContinueOnTrail.cs`; cleared via main-menu Options EraseTombstone (`TombstoneModule.Reset`).

> **Where persistent data lives: nowhere on disk.** Both high scores and tombstones are in-memory only; all game data is lost when the process exits.

**End-game scoring lives in `src/Window/GameOver/FinalPoints.cs`, not in `ScoringModule`.** It sums points for living-passenger health, the wagon, oxen, spare parts, clothes, bullets, food, and cash (each item's points = `SimItem.Points` = `Quantity/PointsPerAmount*PointsAwarded`), then multiplies by the leader's `Profession` (**Banker ×1, Carpenter ×2, Farmer ×3**) and calls `Scoring.Add(...)`. Ratings: Greenhorn `<3000`, Adventurer `3000–6999`, TrailGuide `>=7000`. **Edit scoring rules here.** The profession `switch` throws `ArgumentOutOfRangeException` for any other `Profession` value — adding a profession crashes end-game scoring.

### Entities — `src/Entity/`
The domain model, all implementing `IEntity` (`Name` + WolfCurses `ITick`) so they can be ticked and passed generically to the event director. **Identity/equality is by `Name` string only** — two items with the same `Name` are "equal"; renaming or duplicating names silently breaks lookups. Most game tuning lives here:
- `SimItem` (`src/Entity/Item/SimItem.cs`) is the single universal class for **every** commodity and for abstract "reference" entities (Cash/Vehicle/Person). The `Entities` enum (`Entities.cs`) is the category tag; the Vehicle inventory is a `Dictionary<Entities, SimItem>` (one slot per category).
- Item tuning constants are literal ctor args in three static factories: `Resources.cs` (consumables + Cash/Person/Vehicle refs), `Parts.cs` (Oxen/Axle/Tongue/Wheel), `Animals.cs` (hunting yields). **Each property access allocates a NEW `SimItem`** (`=> new SimItem(...)`) — treat them as constructors, never compare by reference; `Animals.*` getters call `GameSimulationApp.Instance.Random` so they require an active journey context.
- `Vehicle` (`src/Entity/Vehicle/Vehicle.cs`) is the aggregate root: `_inventory`, `_parts` (4 wheels/1 axle/1 tongue), `_passengers`. **Money is not a field** — `Balance` is a computed wrapper over `Inventory[Entities.Cash]`. `DefaultParts`/`CreateRandomItem` use a `switch` over `Entities` with `default: throw` — adding an `Entities` member without updating these throws at runtime.
- `Person` (`src/Entity/Person/Person.cs`): health is a hidden `0–500` int exposed only as the banded `HealthStatus` enum (Good=500…Dead=0). `FoodRations.PoundsPerPerson` defines daily packed-food use: **Filling=2 lb, Meager=1.5 lb, BareBones=1 lb**. `Vehicle.TryConsumeMeal` shares half-pound portions across passengers; `FoodPerDay` and `FoodPoundsRemaining` supply the browser's decimal forecasts. Ration enum values still control illness exposure, not food weight.
- `Location` (`src/Entity/Location/`): only `Settlement` returns true for `ShoppingAllowed`/`ChattingAllowed`. Each simulates `Weather` from a `Climate` enum (monthly tuning in `ClimateData`).

### Event system — `src/Event/` + `src/Module/Director/`
Random/scripted incidents are individual classes under `src/Event/<Category>/`, each derived from `EventProduct` (directly or via a prefab) and tagged `[DirectorEvent(EventCategory.X)]`. `EventFactory` discovers them **by reflection** at construction — there is no central registration list. The six `EventCategory` values (`src/Event/EventCategory.cs`) are `Vehicle`, `Animal`, `Person`, `Weather`, `Wild`, **`RiverCross`** (note: the enum is `RiverCross`, though the folder is `src/Event/River/`).

`EventDirectorModule.TriggerEventByType(source, category)` rolls a **per-category weighted chance** (`CategoryChance`; the base game's flat 1% was replaced) and picks a random event of that category (biased away from the last 3 fired); `TriggerEvent(source, type)` fires a specific event unconditionally. Where each category is rolled: Weather → `LocationWeather.cs`, Person → `Person.cs`, Vehicle → `Vehicle.cs`, RiverCross → `CrossingTick.cs`, and **`Wild` (4%), `Animal` (2%), and `ModernHazard` (1%) → `ContinueOnTrail.cs`** once per moving travel day. (Older notes calling `Animal`/`Wild` "dead content" are stale — the 2028 re-skin wired them into the travel tick.) **`ModernHazard`** is the 2028 difficulty lever: a weighted spread of satirical modern deaths/catastrophes (`src/Event/Modern/` skins over five `src/Event/Prefab/Modern*` effect profiles), tuned with the live-source runner in `tools/strategy-sim/` toward roughly 45% whole-family arrival for the documented prepared minivan policy. Random crossing incidents roll at **0.5% per crossing tick**. Steady/Strenuous/Grueling mileage factors are **1.25/1.3/1.6**; Steady avoids extra fatigue. The older `sim/Program.cs` approximation is not the balance authority. See `DESIGN_2028_AMERICAN_ROADTRIP.md` §11.

**Prefab bases** (abstract, in `src/Event/Prefab/`, excluded from the registry): `ItemDestroyer`, `ItemCreator`, `PersonInjure`, `PersonInfect`, `FoodDestroyer`, `LoseTime` — subclass and override a couple of hooks (`OnPreDestroyItems`/`OnPostInjury`/`DaysToSkip`/etc.).

The `RandomEvent` window's `EventExecutor` form runs `Execute → Render → OnPostExecute`, then `OnEventClose` on dismissal.

## Common tasks (recipes)

**Add a random event:** create a class under `src/Event/<Category>/`; derive from `EventProduct` (or a prefab base); tag `[DirectorEvent(EventCategory.X)]` (add `, EventExecution.ManualOnly` to exclude it from random category rolls — it then only fires via `TriggerEvent(typeof(...))`); implement the abstract hooks. Reflection picks it up automatically. **Gotchas:** events are instantiated via `GetUninitializedObject` — the constructor does **not** run, so put setup in `OnEventCreate()` (call `base.OnEventCreate()`); `Render` **must** return non-empty text or `EventExecutor` throws; `EventKey` equality is by **class name only**, so names must be globally unique or the factory silently drops the duplicate.

**Add a screen (Form) inside an existing Window:** `class MyScreen : InputForm<TheWindowData>` (or `Form<TheWindowData>` for custom rendering / free-text); decorate `[ParentWindow(typeof(TheWindow))]`; ctor `public MyScreen(IWindow window) : base(window) {}`; override `OnDialogPrompt`/`OnDialogResponse` (dialog) or `OnRenderForm` + `InputFillsBuffer => true` + `OnInputBufferReturned` (custom/typed input); reach it via `SetForm(typeof(MyScreen))` (usually from an `AddCommand` handler); end with `ClearForm()` or `SetForm(next)`.

**Add a top-level Window:** `class MyWindow : Window<MyCommands, MyData>`; create `enum MyCommands` with `[Description]` labels and `class MyData : WindowData`; register commands in `OnWindowPostCreate` via `AddCommand(handler, MyCommands.X)`; **CRITICAL: add `typeof(MyWindow)` to `AllowedWindows` in `src/GameSimulationApp.cs`** or the factory silently won't create it; push with `WindowManager.Add(typeof(MyWindow))`, pop self with `RemoveWindowNextTick()`.

**Add/edit a trail:** edit `src/Module/Trail/TrailRegistry.cs` (a static property returning an ordered `Location[]` wrapped in `new Trail(array, lengthMin, lengthMax)`); to switch the active trail, change the `Trail = TrailRegistry.AsphaltTrail` line in `TrailModule`'s ctor.

## Conventions

- Namespaces mirror folders under the `OregonTrailDotNet` root (e.g. `OregonTrailDotNet.Window.Travel.Store`).
- "Windows" / "modes" in comments and method names mean these **state-machine windows, not the OS**.
- Top-level Windows are a hardcoded whitelist (`AllowedWindows`); events self-register by reflection (`[DirectorEvent]`). Know which registration model applies before adding either.
- The web interface uses a terminal and ASCII visual language. Keep gameplay in semantic responsive elements; reserve preformatted text for decorative, aria-hidden character art in `scenes.js`. Never introduce horizontal gameplay scrolling or parse terminal frames for game state.

## Local credentials (NOT in the repo)

A **Google AI (Gemini) API key** is stored locally at `~/.config/asphalt-trail/google_ai.env`
(chmod 600, **outside** the repo — the key value is *not* committed and must never be). Load it with:

```bash
set -a; source ~/.config/asphalt-trail/google_ai.env; set +a   # exports GOOGLE_AI_API_KEY
```

- Authenticates as a standard **Gemini API key** via the `x-goog-api-key` header against
  `generativelanguage.googleapis.com` (verified 200; `Authorization: Bearer` returns 401, so it is
  *not* an OAuth token). For Google client libs, also export it as `GEMINI_API_KEY`/`GOOGLE_API_KEY`.
- Image generation ("**nano banana**") models available to this key: `gemini-2.5-flash-image`
  (nano banana) and `gemini-3-pro-image` (nano banana pro); also `gemini-3.1-flash-image` and
  `imagen-4.0-*`. Used for experiments like generating the smiling-sun logo art.
- The key is also visible in chat history where it was first pasted — **rotate it in Google AI
  Studio** if anything sensitive ever rides on it.

## Web deployment (NOT in the repo)

The intended public URL remains `https://wagenhoffer.dev/oregon`, but the checked-in deployment design is now a native ASP.NET reverse-proxy target rather than ttyd. See [`DEPLOYMENT.md`](./DEPLOYMENT.md). `GameSession` keeps cookie-keyed in-memory workers, bounded to 256 by default; replicas require sticky routing. Scores, tombstones, and journeys remain ephemeral. See the worker harness in `tests/Backend/` and the real HTTP integration checks in `tests/backend_integration.py`.
