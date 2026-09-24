# Game strategy results — September 24, 2026 (before rebalance)

These measurements describe the original rules. See [the subsequent family-survival rebalance](../family-balance/report.md) for current results.

The most reliable tested approach is **crypto bro (Banker) + Beige Minivan + maximum fuel + Filling rations + Strenuous or Grueling pace**, with regular supply stops and cautious crossings. The stronger minivan policies reached Seattle in about **71–73%** of independent validation journeys. This is arrival with at least one survivor; preserving the entire family is much harder.

I ran **8,200 measured journeys through the current C# game rules**: 32 pilot policies × 100 seeds, followed by 10 validation policies × 500 fresh seeds. No measured run ended in a harness error or timeout. A separate 10-seed replay produced identical outcomes. The old `sim/Program.cs` was not used: it has outdated food prices, disables modern hazards by default, and omits crossings and story choices.

## Validated results

Each row uses seeds 10001–10500. Intervals are 95% Wilson intervals for arrival probability. Within the faster minivan group, the small differences do not establish a clear winner. These are tested policies, not proven global optima.

| Background / vehicle / pace | Arrival | 95% interval | Median days, winners | Entire starting party arrives, all trials |
|---|---:|---:|---:|---:|
| Crypto bro / minivan / Grueling / 11 leggings | **72.6%** (363/500) | 68.5–76.3% | 34 | 20.0% (4 people) |
| Crypto bro / minivan / Grueling / 7 leggings | 71.8% | 67.7–75.6% | 34 | 18.0% (4 people) |
| Crypto bro / minivan / Grueling / 4 leggings | 70.6% | 66.5–74.4% | 36 | 17.2% (4 people) |
| Crypto bro / minivan / Strenuous / 4 leggings | **70.6%** | 66.5–74.4% | 37 | 18.2% (4 people) |
| Crypto bro / hybrid / Grueling | 64.8% | 60.5–68.9% | 35 | 21.2% (3 people) |
| Crypto bro / EV / Grueling | 62.8% | 58.5–66.9% | 35 | 19.0% (3 people) |
| DoorDash-driver background / minivan / Grueling | 61.0% | 56.7–65.2% | 36 | 14.8% (4 people) |
| Crypto bro / minivan / Steady | 50.4% | 46.0–54.8% | 44 | 8.8% (4 people) |
| Faith-walk streamer / minivan / Grueling / resellables | 24.2% | 20.6–28.1% | 34 | 2.0% (4 people) |

Vehicles use budget-appropriate supply targets, so the vehicle rows compare complete strategies rather than isolating vehicle speed. The hybrid/EV and DoorDash-driver policies omit the $1,200 spare transmission. The streamer omits all spare parts and takes the opening resellables choice for $600 extra cash, which also removes 80 food at opening checkout.

## A repeatable opening

For the top observed minivan setup, choose the **crypto bro**, **Beige Minivan**, **March**, and **Travel light**. Buy:

| Item | Quantity | Opening cost |
|---|---:|---:|
| Gas cans | 20 | $500 |
| MLM leggings | 11 | $825 |
| Snacks | 289 lb | $578 |
| Tires | 2 | $400 |
| Alternator | 1 | $300 |
| Transmission | 1 | $1,200 |
| Total supplies | | **$3,803** |

The vehicle costs $1,200 out of the $8,000 starting cash, leaving **$2,997 after these supplies**. Leggings and food fill the 300-unit cargo limit. Use **Grueling pace and Filling rations**. At stores, refill gas, clothing, food, then spares to these targets while cash allows.

A simpler, cheaper alternative is **4 leggings + 296 food**, retaining **$3,508** after opening purchases. Either Strenuous or Grueling reached Seattle in 70.6% of validation trials with that load. Extra leggings permit some guide payments while retaining clothing afterward; their incremental survival benefit was not resolved conclusively by the sample.

Keep the four-person minivan party. Take **Big Texan Steak Ranch** at the Texas fork and **Tacoma** at the Cascades fork, as in these trials. These routes retain supply opportunities and avoid the additional Gorge branch. March and the fork choices were held fixed, so the experiment does not establish an optimal departure month or independently measure route differences.

## Crossings and story choices

Use the **National Guard convoy** when available and affordable. Otherwise hire the local if you have enough leggings; otherwise wait until the displayed depth is **5 feet or less**, then take the **sealed-door shoulder detour**. Paid/guided crossings still have ordinary random incidents. Gunning it through more than 3 feet triggers a scripted washout; taking the shoulder detour above 5 feet adds a flooding risk. Crossing ticks can also trigger illness, even without consuming a calendar day.

When story decisions appear:

- **Buc-ee's:** stay disciplined; the implemented effect is −$40 and +80 food. Despite the wording about topping off, this choice itself does not add gas cans.
- **Carhenge:** join the caravan. It costs food and enables the safe checkpoint assertion branch. The dialogue mentions slower travel, but the current vehicle mileage calculation does not apply a caravan speed penalty.
- **Walmart:** buy the **$120 trauma kit** to restore surviving passengers to full health. It cannot resurrect dead passengers.
- **Checkpoint:** **assert if you joined the caravan; otherwise comply**. Asserting alone and unarmed costs 150 food and four days. Do not assume the caravan choice appeared: some location stories depend on the travel window being reactivated and are skipped on some journeys.

For score, the armed-caravan assertion branch awards a large conditional bonus, but buying the gun is not a reliable survival improvement. The gun variant of the 4-legging Grueling policy won 68.2% versus 70.6% with the trauma kit, with overlapping uncertainty intervals. Its median winning score was also lower in this sample because the prerequisite stories are not guaranteed. The **DoorDash-driver background** is a clearer score tradeoff: its tested setup had a median winning score of **3,272**, versus **1,890** for the 11-legging Banker setup, at a lower arrival rate. This refers to the starting background; no delivery shifts were worked.

## What to avoid

These pilot comparisons each used 100 seeds and a Banker/minivan/Steady baseline, changing the named setting only (food increases to 300 when clothing is removed). They are exploratory estimates, not the 500-run validation rates above.

| Policy | Pilot arrival rate |
|---|---:|
| Baseline: 20 gas, Filling, 4 leggings, refill stores, cautious crossings | 56% |
| Only 8 gas | **9%** |
| Only 12 gas | 23% |
| Meager rations | 35% |
| Bare Bones rations | **2%** |
| No clothing | 47% |
| No store refills | 23% |
| Always shoulder-detour immediately, regardless of depth | 15% |
| Always gun it through the water | **6%** |

Fuel inventory contributes directly to the daily mileage calculation; ordinary driving does not decrement gas each day. Too little fuel slows the journey and exposes the party to more hazards. Filling rations consume more food but avoid the additional ration-related illness checks. Faster paces damage health, but their shorter journeys performed better here. The game includes outright whole-party catastrophes, so a well-supplied group can still lose instantly.

## Reproduction and scope

The [runner documentation](../../tools/strategy-sim/README.md) includes build commands and all policy flags. Raw data and summaries are in [pilot/summary.json](pilot/summary.json) and [validation/summary.json](validation/summary.json); [validation-policies.json](validation-policies.json) gives all final overrides. [provenance.json](provenance.json) records the source fingerprint, including the pre-existing uncommitted workspace changes used by these runs.

The harness replaces wall-clock delays with fixed ticks, then executes the actual menus, purchases, travel, weather, illness, event effects, crossing waits, route insertion, and scoring. It uses the game's normal prices and probabilities. **No production game rules were changed.** The strategy does not use trading, food sweeps, DoorDash, crypto, creator income, or optional recovery rests. An unrepaired disabled car is counted as a failed arrival; a human might still rescue it by trading. Results describe these policies on the current local rules, not all possible strategies or a verified deployed build.
