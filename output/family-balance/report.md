# Steady pace, food, and whole-family survival rebalance

The previously recommended four-person minivan loadout now brings the **entire family to Seattle in 47.6% of journeys**, close to the requested 45% target, while using **Steady** pace. The same setup previously managed 12.8% at Steady. These are whole-family successes divided by **all started trials**, not just winning journeys.

## Implemented changes

| Rule | Before | After |
|---|---:|---:|
| Steady mileage factor | 1.0 | **1.25**, applied to every vehicle |
| Strenuous / Grueling mileage factors | 1.3 / 1.6 | 1.3 / 1.6 |
| Modern-hazard roll per moving day | 9% | **1%** |
| Random crossing-incident roll per crossing tick | 1% | **0.5%** |
| Filling food per person/day | 3 lb | **2 lb** |
| Meager food per person/day | 2 lb | **1.5 lb** |
| Bare Bones food per person/day | 1 lb | 1 lb |

Steady retains no extra fatigue penalty. Harder paces retain their speed advantages and existing health costs. Modern events still have their authored consequences; they occur less often. Paid crossings still have some risk, and dangerous deep-water choices still trigger their scripted consequences. Other event-category probabilities are unchanged.

A four-person family on Filling now uses **8 lb/day instead of 12**, a one-third reduction. Meager uses 6 lb/day and Bare Bones uses 4. Food weights represent packed provisions, not a nutritional claim about all kinds of food. The existing illness tradeoff for smaller meals remains.

Half-pound portions are conserved across passengers and ration changes rather than rounded up per passenger. The last partial meal is usable, an empty pantry still causes hunger, and restart clears leftovers. Opened food reserves cargo space until eaten. The HUD, ration-choice labels, and checkout forecast use the new rates, including fractional rates for odd-size parties. Shop purchases and prices remain whole-pound/whole-dollar quantities.

## Independent before/after validation

**2,000 trials per version, seeds 40001–42000**, using the live C# engine. The old executable was preserved before editing the rules. The same policy was used in both versions:

- Crypto-bro/Banker budget, Beige Minivan, March, four passengers, Travel light.
- 20 gas, 11 leggings, 289 food, 2 tires, 1 alternator, 1 transmission; refill at stores as funds allow.
- Steady pace and Filling rations; Big Texan Steak Ranch and Tacoma forks.
- Pay for the Guard convoy when possible, otherwise hire an affordable guide, otherwise wait to 5 feet and take the shoulder detour.
- Disciplined Buc-ee's option, caravan, trauma kit, and assert at the checkpoint only if the caravan was joined, when these choices appear.

| Outcome | Before | After |
|---|---:|---:|
| Entire family arrives | 256/2000 = **12.8%** | 952/2000 = **47.6%** |
| 95% Wilson interval, whole-family arrival | 11.4–14.3% | **45.4–49.8%** |
| At least one survivor arrives | **53.15%** | **87.2%** |
| Median days among winners | 44 | **36** |
| Mean survivors among winners | 2.41 | **3.06** |

This preserves the previous recommended loadout, rather than introducing a new optimized policy after observing validation seeds. Initial tuning used separate seeds 20001–23500; the final rules were fixed before the 40001 validation cohort. The final source build also reproduced the first ten validation results exactly.

## Other strategies still matter

These independent checks use 500 trials each, seeds 45001–45500. The target is specific to the reference policy; it is not a guarantee for every background or supply load.

| Policy | Any-survivor arrival | Entire starting party arrives |
|---|---:|---:|
| Banker minivan, Steady, 4 leggings | 86.2% | 38.6% |
| Banker minivan, Strenuous, 4 leggings | 84.4% | 41.0% |
| Banker minivan, Grueling, 11 leggings | 89.4% | 47.0% |
| DoorDash-driver background, minivan, Steady | 77.2% | 36.8% |
| Streamer background, resellables, minivan, Steady | 53.6% | 20.4% |
| Banker hybrid, Meager, Steady, three passengers | 71.0% | 22.0% |

The 4-legging load reserves less clothing for the guide and later losses. The DoorDash-driver policy omits the spare transmission; the streamer omits all spare parts and uses the opening resellables cash. The hybrid policy also omits the transmission. Exact overrides are recorded in `other-policies.json`. The slower and faster minivan policies now have broadly comparable arrival rates, so pushing the family harder is no longer necessary for reasonable success.

## Checks and reproduction

Passed: Release builds; food conservation/forecast tests; the full backend harness; web contract/assets/revision/isolation verification; real HTTP setup, driving, and fractional ration-forecast checks; JavaScript syntax; whitespace checks. All final measured trials completed without harness errors or timeouts.

```bash
dotnet build tools/strategy-sim/StrategySim.csproj -c Release --no-restore
python3 tools/strategy-sim/sweep.py --trials 2000 --seed 40001 \
  --config output/family-balance/target-policy.json \
  --output output/family-balance/recheck

dotnet run --project tests/Backend/BackendTests.csproj -c Release --no-restore -- --food-rules
```

Raw outcomes and summaries are in `before-target/`, `after-target/`, and `after-other/`. `provenance.json` records the final source fingerprint and saved old executable hash. `tuning-1/` through `tuning-4/` contain exploratory candidates; their settings are listed in `tuning-candidates.json` and are not the final validation sample. `before/` is an additional old-rules 4-legging baseline.

The runner advances real game ticks without wall-clock delays and uses actual shopping, illness, events, crossings, and scoring. It does not use trading, food sweeps, delivery shifts, crypto, creator income, or optional recovery rests. An unrepaired disabled car is a failed arrival; trading might rescue it in human play. The historical approximation in `sim/Program.cs` is not used for this balance. Results refer to the local rules, not a separately verified deployed build.
