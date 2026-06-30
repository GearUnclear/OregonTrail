# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A console (text-UI) clone of the 1990s *Oregon Trail* game, written in C# on **.NET 6** (`net6.0`, `OutputType=Exe`). It is a single executable project (`src/OregonTrailDotNet.csproj`) that depends on the **WolfCurses** NuGet package (`WolfCurses` 2018.9.12.1) — a separate TUI/state-machine engine that provides the base classes this game extends: `SimulationApp`, `Window`, `Form`, `Module`, the `SceneGraph` renderer, `WindowManager`, and `InputManager`. There is no engine source or git submodule in this repo; WolfCurses is consumed purely as a `PackageReference`.

> The `README.md` is stale: it describes Cake build scripts (`build.sh`/`build.bat`) and recursive git submodules. Neither exists anymore — WolfCurses is a plain NuGet reference and the build is ordinary `dotnet`. The `tools/` Cake config is likewise vestigial.

## Build & Run

```bash
dotnet restore src/OregonTrailDotNet.csproj
dotnet build   src/OregonTrailDotNet.csproj -o ./bin
dotnet run --project src/OregonTrailDotNet.csproj
```

**OutputPath gotcha:** `OregonTrailDotNet.csproj` hardcodes `<OutputPath>C:\OregonTrail\bin\</OutputPath>` for **both** Debug and Release. This Windows path fails or writes to an odd location off-Windows. Pass `-o ./bin` (or delete those two `PropertyGroup`s) when building on Linux/macOS.

**No test project.** The game is interactive and **cannot run headlessly**: the loop in `Program.cs` depends on `Console.KeyAvailable`, `Console.ReadKey`, `Console.WindowHeight/WindowWidth`, and `SetCursorPosition`, which throw or misbehave in a redirected/CI terminal. `Console.OutputEncoding = Unicode` is required for the box/progress-bar glyphs. It needs a real TTY.

## Architecture

The whole game is a hierarchical state machine driven by the WolfCurses engine. The four layers below are the key to navigating the code.

### 1. Simulation singleton — `GameSimulationApp` (`src/GameSimulationApp.cs`)
Extends `WolfCurses.SimulationApp`; a hard singleton accessed everywhere via `GameSimulationApp.Instance`. Created only through the static `Create()` factory (which throws `InvalidOperationException` if `Instance` is already non-null) and destroyed only by `OnPreDestroy()` setting `Instance = null`. It owns:
- The game **Modules** (see layer 4) and the active **Vehicle** entity.
- `AllowedWindows` (~lines 84–99) — the whitelist of top-level `Window` types the `WindowManager` may instantiate: `Travel`, `MainMenu`, `RandomEvent`, `Graveyard`, `GameOver`.
- `MAXPLAYERS = 4` — caps party size and how many names the new-game flow collects.

**Module lifetime is split (load-bearing):**
- `Scoring` and `Tombstone` are constructed **once** in `OnPostCreate()` and **survive `Restart()`** — they are only cleared via the main-menu Options screens.
- `Time`, `EventDirector`, `Trail`, and the `Vehicle` are rebuilt **every** `Restart()` (constructed in that order; `Time` must tick first). `Restart()` then adds `Travel` (bottom/always-present) and pushes `MainMenu` on top.

**Lifecycle chain:** `OnPostCreate` (persistent modules) → `OnFirstTick` (calls `Restart`) → `Restart` (per-game state + window stack) → `OnPreRender` (per-frame: prepends the `Turns: NNNN` / `Vehicle … Location …` status header) → `OnPreDestroy` (teardown).

`Program.cs` is the **only OS-facing code**. It is a busy-wait loop `while (Instance != null) { OnTick(true); poll key; Thread.Sleep(1); }`. Input maps exactly three ways: **Enter → `InputManager.SendInputBufferAsCommand()`**, **Backspace → `RemoveLastCharOfInputBuffer()`**, **any other char → `AddCharToInputBuffer(key.KeyChar)`** (keybindings live here). It paints the full screen on every `SceneGraph.ScreenBufferDirtyEvent` (no diffing). **CTRL-C does not kill the process** — `Console_CancelKeyPress` sets `e.Cancel = true` and calls `Instance.Destroy()`; the loop exits only because `OnPreDestroy` nulls `Instance`.

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
- **Time** (`src/Module/Time/`) — master day/month/year clock; start **1848 / March / day 1**; fixed **30-day months** (`Date.NumberOfDaysInMonth = 30`), **no leap years**. `TakeTurn(skipDay=false)` → `TimeModule.TickTime` → `OnTickDay` → `Trail.OnTick`. Nothing on the trail moves except via the time tick. `skipDay=true` ticks/fires events without consuming a calendar day or incrementing `TotalTurns`.
- **Trail** (`src/Module/Trail/`) — `TrailRegistry.cs` declares each trail as a `Location[]` (ordered `Settlement`/`RiverCrossing`/`Landmark`/`ForkInRoad`/`TollRoad`, each with a `Climate`) wrapped in `new Trail(locations, lengthMin, lengthMax)`. Segment distances are **randomized per game** in the band (32–164 for Oregon). `TrailModule` hardcodes `Trail = TrailRegistry.OregonTrail` in its ctor and tracks `LocationIndex`/`DistanceToNextLocation`. `NextLocation == null` signals end of game.
- **Director** (`src/Module/Director/`) — the event engine (see below).
- **Scoring** (`src/Module/Scoring/`) — a ranking **container only** (`List<Highscore>`, `TopTen`, `Add`/`Reset`), seeded from hardcoded `DefaultTopTen`. **It does NOT compute the score** (that's `FinalPoints.cs`, below). **Not persisted to disk** — `Destroy`/`Reset` are `// TODO: Save/Load … as JSON` stubs; player scores re-seed from defaults every launch.
- **Tombstone** (`src/Module/Tombstone/`) — in-memory `Dictionary<int, Tombstone>` keyed by `Vehicle.Odometer` mile marker (one grave max per marker). **NOT persisted to disk** despite class docstrings — the ctor has `// TODO: Need to code JSON saving and loading`. Graves survive a `Restart()` within one process run but are **lost on process exit**. Rediscovered during travel in `src/Window/Travel/Command/ContinueOnTrail.cs`; cleared via main-menu Options EraseTombstone (`TombstoneModule.Reset`).

> **Where persistent data lives: nowhere on disk.** Both high scores and tombstones are in-memory only; all game data is lost when the process exits.

**End-game scoring lives in `src/Window/GameOver/FinalPoints.cs`, not in `ScoringModule`.** It sums points for living-passenger health, the wagon, oxen, spare parts, clothes, bullets, food, and cash (each item's points = `SimItem.Points` = `Quantity/PointsPerAmount*PointsAwarded`), then multiplies by the leader's `Profession` (**Banker ×1, Carpenter ×2, Farmer ×3**) and calls `Scoring.Add(...)`. Ratings: Greenhorn `<3000`, Adventurer `3000–6999`, TrailGuide `>=7000`. **Edit scoring rules here.** The profession `switch` throws `ArgumentOutOfRangeException` for any other `Profession` value — adding a profession crashes end-game scoring.

### Entities — `src/Entity/`
The domain model, all implementing `IEntity` (`Name` + WolfCurses `ITick`) so they can be ticked and passed generically to the event director. **Identity/equality is by `Name` string only** — two items with the same `Name` are "equal"; renaming or duplicating names silently breaks lookups. Most game tuning lives here:
- `SimItem` (`src/Entity/Item/SimItem.cs`) is the single universal class for **every** commodity and for abstract "reference" entities (Cash/Vehicle/Person). The `Entities` enum (`Entities.cs`) is the category tag; the Vehicle inventory is a `Dictionary<Entities, SimItem>` (one slot per category).
- Item tuning constants are literal ctor args in three static factories: `Resources.cs` (consumables + Cash/Person/Vehicle refs), `Parts.cs` (Oxen/Axle/Tongue/Wheel), `Animals.cs` (hunting yields). **Each property access allocates a NEW `SimItem`** (`=> new SimItem(...)`) — treat them as constructors, never compare by reference; `Animals.*` getters call `GameSimulationApp.Instance.Random` so they require the singleton to exist.
- `Vehicle` (`src/Entity/Vehicle/Vehicle.cs`) is the aggregate root: `_inventory`, `_parts` (4 wheels/1 axle/1 tongue), `_passengers`. **Money is not a field** — `Balance` is a computed wrapper over `Inventory[Entities.Cash]`. `DefaultParts`/`CreateRandomItem` use a `switch` over `Entities` with `default: throw` — adding an `Entities` member without updating these throws at runtime.
- `Person` (`src/Entity/Person/Person.cs`): health is a hidden `0–500` int exposed only as the banded `HealthStatus` enum (Good=500…Dead=0). `RationLevel`'s int value (**Filling=1, Meager=2, BareBones=3**) doubles as the per-person daily food-consumption multiplier — changing it silently rebalances food.
- `Location` (`src/Entity/Location/`): only `Settlement` returns true for `ShoppingAllowed`/`ChattingAllowed`. Each simulates `Weather` from a `Climate` enum (monthly tuning in `ClimateData`).

### Event system — `src/Event/` + `src/Module/Director/`
Random/scripted incidents are individual classes under `src/Event/<Category>/`, each derived from `EventProduct` (directly or via a prefab) and tagged `[DirectorEvent(EventCategory.X)]`. `EventFactory` discovers them **by reflection** at construction — there is no central registration list. The six `EventCategory` values (`src/Event/EventCategory.cs`) are `Vehicle`, `Animal`, `Person`, `Weather`, `Wild`, **`RiverCross`** (note: the enum is `RiverCross`, though the folder is `src/Event/River/`).

`EventDirectorModule.TriggerEventByType(source, category)` fires **only when `Random.Next(100) == 0`** (~1% per call, not tunable); `TriggerEvent(source, type)` fires a specific event unconditionally. The **only** places `TriggerEventByType` is called: Weather → `LocationWeather.cs`, Person → `Person.cs`, Vehicle → `Vehicle.cs`, RiverCross → `CrossingTick.cs`. **`Animal` and `Wild` are dead content** — nothing rolls those categories, so those events register but never fire in normal play.

**Prefab bases** (abstract, in `src/Event/Prefab/`, excluded from the registry): `ItemDestroyer`, `ItemCreator`, `PersonInjure`, `PersonInfect`, `FoodDestroyer`, `LoseTime` — subclass and override a couple of hooks (`OnPreDestroyItems`/`OnPostInjury`/`DaysToSkip`/etc.).

The `RandomEvent` window's `EventExecutor` form runs `Execute → Render → OnPostExecute`, then `OnEventClose` on dismissal.

## Common tasks (recipes)

**Add a random event:** create a class under `src/Event/<Category>/`; derive from `EventProduct` (or a prefab base); tag `[DirectorEvent(EventCategory.X)]` (add `, EventExecution.ManualOnly` to exclude it from random category rolls — it then only fires via `TriggerEvent(typeof(...))`); implement the abstract hooks. Reflection picks it up automatically. **Gotchas:** events are instantiated via `GetUninitializedObject` — the constructor does **not** run, so put setup in `OnEventCreate()` (call `base.OnEventCreate()`); `Render` **must** return non-empty text or `EventExecutor` throws; `EventKey` equality is by **class name only**, so names must be globally unique or the factory silently drops the duplicate.

**Add a screen (Form) inside an existing Window:** `class MyScreen : InputForm<TheWindowData>` (or `Form<TheWindowData>` for custom rendering / free-text); decorate `[ParentWindow(typeof(TheWindow))]`; ctor `public MyScreen(IWindow window) : base(window) {}`; override `OnDialogPrompt`/`OnDialogResponse` (dialog) or `OnRenderForm` + `InputFillsBuffer => true` + `OnInputBufferReturned` (custom/typed input); reach it via `SetForm(typeof(MyScreen))` (usually from an `AddCommand` handler); end with `ClearForm()` or `SetForm(next)`.

**Add a top-level Window:** `class MyWindow : Window<MyCommands, MyData>`; create `enum MyCommands` with `[Description]` labels and `class MyData : WindowData`; register commands in `OnWindowPostCreate` via `AddCommand(handler, MyCommands.X)`; **CRITICAL: add `typeof(MyWindow)` to `AllowedWindows` in `src/GameSimulationApp.cs`** or the factory silently won't create it; push with `WindowManager.Add(typeof(MyWindow))`, pop self with `RemoveWindowNextTick()`.

**Add/edit a trail:** edit `src/Module/Trail/TrailRegistry.cs` (a static property returning an ordered `Location[]` wrapped in `new Trail(array, lengthMin, lengthMax)`); to switch the active trail, change the `Trail = TrailRegistry.OregonTrail` line in `TrailModule`'s ctor.

## Conventions

- Namespaces mirror folders under the `OregonTrailDotNet` root (e.g. `OregonTrailDotNet.Window.Travel.Store`).
- "Windows" / "modes" in comments and method names mean these **state-machine windows, not the OS**.
- Top-level Windows are a hardcoded whitelist (`AllowedWindows`); events self-register by reflection (`[DirectorEvent]`). Know which registration model applies before adding either.

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