# Strategy experiments against the live game

This runner compiles the current `src/` C# rules into a separate executable. It does not use the older approximation in `sim/Program.cs` and does not change game tuning. WolfCurses discovers forms in the entry assembly, which is why this project includes the sources instead of referencing the main executable.

Build using the locally cached dependency:

```bash
dotnet restore tools/strategy-sim/StrategySim.csproj --source /root/.nuget/packages -p:NuGetAudit=false
dotnet build tools/strategy-sim/StrategySim.csproj -c Release --no-restore
```

On a machine without that cache, use the normal `dotnet restore` instead.

Run one traced journey or repeat a strategy:

```bash
dotnet tools/strategy-sim/bin/Release/net8.0/OregonTrailDotNet.dll --pace 3 --seed 201 --trace 1
dotnet tools/strategy-sim/bin/Release/net8.0/OregonTrailDotNet.dll --pace 3 --seed 10001 --trials 500
python3 tools/strategy-sim/sweep.py --trials 100
python3 tools/strategy-sim/sweep.py --trials 500 --seed 10001 \
  --config output/strategy-analysis/validation-policies.json \
  --output output/strategy-analysis/validation
```

The executable emits one JSON outcome per seed. Trace messages go to stderr. The Python sweep stores raw outcomes and a summary with Wilson 95% binomial confidence intervals for both arrival and whole-party arrival. `full_party_pct` uses all trials as its denominator. Use `--dll /path/to/previous/OregonTrailDotNet.dll` to compare a saved build. It treats errors/timeouts as errors requiring investigation, not ordinary game losses.

## Policy

Defaults: Banker/crypto bro; minivan; March; full vehicle party; travel light; 20 gas, 4 leggings, 296 snacks, 2 tires, 1 alternator, 1 transmission; Steady pace; Filling rations. Stores refill to these targets while cash and cargo allow, prioritizing gas, clothing, food, then spares. Targets are not guaranteed purchases when funds are insufficient. `--food` is also limited to vehicle capacity minus the clothing target. No ammunition is bought.

Story options when offered: disciplined Buc-ee's purchase; join caravan; trauma kit; assert at checkpoint if the caravan was joined, otherwise comply. Forks take the first destination: Big Texan Steak Ranch, then Tacoma. Crossings pay for the Guard convoy when available and affordable, otherwise hire a local if current leggings cover the price, otherwise wait until depth is at most 5 feet and use the sealed-door shoulder detour. Even paid crossings still roll random crossing incidents.

Options take integer values:

- `--profession`: 1 Banker, 2 Carpenter, 3 Farmer.
- `--vehicle`: 1 minivan, 2 pickup, 3 hybrid, 4 EV.
- `--pace`: 1 Steady, 2 Strenuous, 3 Grueling.
- `--ration`: 1 Filling, 2 Meager, 3 Bare Bones.
- `--gas`, `--clothes`, `--food`, `--tires`, `--alternators`, `--transmissions`: inventory targets.
- `--pack`, `--bucees`, `--caravan`, `--arm`, `--checkpoint`: the corresponding story's numbered choice. Checkpoint option 2 falls back to comply if no caravan was joined.
- `--crossing`: 0 the cautious policy above; 1 always gun it; 2 always take the shoulder detour regardless of depth; 3 skip the Guard convoy, otherwise use the cautious policy.
- `--month`: 1 March through 5 July.
- `--restock`: 1 refill at stores; 0 opening shopping only.
- `--trials`, `--seed`, `--trace`: sample count, first seed, and 0/1 action trace.

## Measurement limits

A win means reaching the score screen at Seattle with at least one living passenger. The runner seeds WolfCurses' existing `System.Random` before initial route generation. It suppresses elapsed wall-clock ticks and advances the same fixed ticks explicitly; the production three-ticks-per-driving-day logic, event dispatch, crossing ticks, menus, purchase calculations, and scoring execute normally. It acts promptly on settled menus and waits for timed activities. Some story beats depend on travel-window reactivation and are not offered every run.

This is a comparison of these policies, not a search of all possible play. It does not use trading, food sweeps, DoorDash shifts, crypto, creator income, or optional medical-rest policies. A disabled car without a usable spare ends the trial as `stranded`; a human might rescue that run by trading. No hidden health or future random outcome is used to make choices. Reflection supplies the RNG seed, advances the test clock, reads displayed crossing/rest state, and invokes the same store quantity setter used by the browser adapter.

The September 2026 rebalance is documented in [the family-survival report](../../output/family-balance/report.md). The earlier strategy-analysis results describe the old rules and should not be treated as current probabilities.
