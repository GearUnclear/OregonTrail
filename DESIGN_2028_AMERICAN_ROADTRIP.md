# Design Spec — *The Asphalt Trail* (2028 American Roadtrip)

> A total **re-skin** of this Oregon Trail clone. Same WolfCurses state machine, same
> windows/forms/modules, **the same odds and formulas byte-for-byte** — only the fiction,
> strings, route, and event flavor change. A family flees **Florida for Seattle in 2028**,
> and every catastrophe is real, sourced, and explained by a passer-by as if it were Tuesday.

---

## 1. Premise & tone

A family loads a paid-off, **climate-uninsurable SUV** in Cape Coral, Florida and drives for
Seattle — not for opportunity, but because Florida's home-insurance market collapsed, the grid
keeps failing, and the interstates keep falling into rivers. The wagon is a used SUV, the oxen
are five-gallon gas cans, the river fords are flooded Interstates and collapsing toll bridges,
and "dysentery" is whatever uniquely American hazard finishes you first.

**The single satirical engine is JUXTAPOSITION, delivered through the existing
`TalkToPeople` / `AdviceRegistry` system.** Every disaster is calmly narrated by a stranger as
the most normal thing in the world — *"Oh yeah, Helene took the highway into the river back in
'24, FEMA says 2028, everybody just reroutes."* The tone is **deadpan and rigorously
non-partisan**: guns, GoFundMes, megachurches, MLMs, civil forfeiture, deep-fried butter, and
street-takeover livestreams are all played straight, as ambient weather. Nothing is
editorialized — the comedy is that to a European it reads as a failed state and to an American
it reads as Tuesday. Every line is sourced (Appendix A); real victims are never the butt of the
joke — only the passer-by's normalizing shrug is.

**Design rule:** if a change touches an *odds table, formula, or threshold*, it is **out of
scope** unless explicitly listed in §9. This is a content/string mod, not a balance mod.

---

## 2. The party & professions

Three professions, reskinned 1:1 onto the existing `Profession` enum (start money & score
multiplier **unchanged** — poorest start still earns the biggest multiplier):

| Original | Becomes | Start $ | Score × |
|---|---|---|---|
| Banker (Boston) | **Crypto Bro (Miami Beach)** — *"Made it in DeFi, lost it twice, still up"* | $1,600 | ×1 |
| Carpenter (Ohio) | **Gig-Economy DoorDash Driver (Ohio)** — *"Five-star rating, no benefits"* | $800 | ×2 |
| Farmer (Illinois) | **Uninsured Twitch Faith-Walk Streamer (Illinois)** — *"Walking to Seattle for the followers and the GoFundMe"* | $400 | ×3 |

> ⚠️ **Keep exactly three.** `src/Window/GameOver/FinalPoints.cs` `switch`es on `Profession`
> with `default: throw new ArgumentOutOfRangeException` — adding a 4th profession crashes
> end-game scoring. The `(int)Profession - 13` term also feeds hunting accuracy, so the enum
> *values* must stay 13/14/15. **Reskin labels only; do not add members.**

---

## 3. Mechanic mapping (the whole game, one row at a time)

Each row: **original mechanic → 2028 reskin → real-world basis → what stays identical in code.**

| Original | Becomes | Real basis | Code preserved |
|---|---|---|---|
| **Oxen** ($20, max 20; empty → `Disabled`) | **5-gal gas cans** ("horsepower"); can't leave first store on empty | Road-trip gas hoarding | `$20`, max 20, `(costAnimals-110)/2.5` Mileage break-even, 50%-halve, floor-of-10, empty→`VehicleStatus.Disabled` all untouched |
| **Food** + **Hunting** (type ShootWord; buffalo 350-500 lb; cap 100) | **Snacks** + **"Buc-ee's / Big Texan / Black Friday Food Sweep"**: type the GRAB word to seize fair food, brisket & door-buster trays | Big Texan 72oz; state-fair deep-fry arms race; Black Friday grabs | `HUNTINGTIME=30`, `MAXPREY=15`, `MAXFOOD=100`, per-prey yields (brisket=buffalo, 72oz steak=caribou), `TryShoot`/`PreyFlee`, `(int)Profession-13` accuracy — byte-identical |
| **Store** (Matt's General Store; 7 items) | **Buc-ee's** (120 pumps) as outfitter — **firearms & ammo in the cart next to the flour** | 74,000 ft² Buc-ee's @ I-40 Exit 407; FL permitless carry | `UpdateStore` skips Cash/Person/Vehicle/Location, sells same 7; `MaxQuantity` caps; `RequiredItem` gate (no depart on 0 gas) unchanged |
| **Ammunition** ($2/box, 20-99) | **Boxes of ammo by the flour** — no license, training, or check | FL 26th permitless state; ammo buyouts | `$2`, min 20, max 99; consumed by bandit (3-15) & snakebite (10); hunting drain formula unchanged |
| **River crossing** | **Flooded Interstate / collapsing toll bridge** (see §4) | I-40→Pigeon River; I-10 floods; Hwy-1 slides | All `RiverGenerator` ranges & disaster thresholds identical |
| **BanditsAttack** (spends 3-15 ammo; loot→`TryKill`) | **Highway-patrol civil-asset-forfeiture stop** — dog "alerts," cash seized, no charge | Stephen Lara's $87k; OK $100k farmland cash | `ItemDestroyer` prefab unchanged; 3-15 spend, "drove them off" fallback; verb reskins seized/forfeited. `Thief` → porch-pirate/repo agent same way |
| **IndiansHelp** (+14 Food) | **Roadside produce-stand family** takes you in, refuses payment | Stricklands hosting Japanese cyclist; Subway gifting SubwaySean | Plain `EventProduct` +14 Food, unchanged |
| **Landmarks** (no store/chat) | **"World's Largest X" shrines**: Cadillac Ranch, Carhenge, Touchdown Jesus, Butter Cow | Real roadside monuments | `Landmark` flags stay false; `AdviceRegistry.Landmark` (12 entries); `Random.Next(32,164)` segment unchanged |
| **Settlements** (only store+chat) | **Buc-ee's, Wall Drug, open-carry Walmart, Portland, Seattle** | Wall Drug; Springfield Walmart; Portland brawls | Only `Settlement` sets `ShoppingAllowed`/`ChattingAllowed=true`; `AdviceRegistry.Settlement` bucket unchanged |
| **Weather/Climate** (Moderate/Continental/Dry/Polar) | **Humid FL / hurricane corridor / heat-dome / wildfire-smoke**; storms → derechos, hurricanes, atmospheric rivers, AQI-484 smoke | Phoenix 31×110°F; Uri; CA atmospheric rivers; NYC orange sky | `LocationWeather.Tick` gating, 1% `Weather` roll, 50% manual storm fires unchanged (enum reskinned, see §9) |
| **Toll road** ($1-12; price gates a fork) | **Dynamic express lanes (I-405) + surprise ambulance billing** — price revealed only after you commit | I-405 dynamic tolls; $9,076 ambulance ride | `TollGenerator.Cost = Random.Next(1,13)`, `>=` check, `InsertLocation` logic identical |
| **Tombstones / Death** (epitaph 38 chars; keyed by odometer) | **Drive-thru open-casket + GoFundMe shoulder headstones** (*"died $50 short of goal"*) | Saginaw drive-thru viewings; Shane Boyle; 91k cancer GoFundMes | `EPITAPH_MAXLENGTH=38`, odometer keying, `DeathPlayer`/`DeathCompanion` manual triggers unchanged |
| **Scoring** (Greenhorn/Adventurer/TrailGuide) | **"Net Worth & Clout Leaderboard"**: Tourist / Influencer / Verified | Diamond-hands clout; GoFundMe as scoreboard | Thresholds 3000/7000, per-item points, `*(int)Profession` bonus unchanged |
| **Professions** | Crypto Bro / DoorDash / Faith-Walk Streamer | (see §2) | `$1600/$800/$400`, ×1/×2/×3, enum values 13/14/15 preserved |
| **Trading** (categories must differ; often 0 trades) | **MLM "hun"** swaps your supplies for crates of unsellable leggings & recruits you | LuLaRoe $4M WA settlement; "LuLaRich" | `GenerateTrades` randoms, category pruning, Disabled-recheck unchanged. **Clothing "sets" = the leggings** used to pay the river guide |
| **Spare parts** (Wheel/Axle/Tongue $10, max 3) | **Tires / alternator / transmission** — break down on a 55-mph shoulder | Sidewalk-less US-highway breakdowns | `DefaultParts`, `$10`, max 3, `BreakRandomPart`, `TryUseSparePart` unchanged |

---

## 4. The headline crossing: flooded Interstates (was: rivers)

Each "river" is a **climate-broken Interstate or a collapsing toll bridge**. The header still
prints two numbers, reskinned: **floodwater depth (ft)** (`RiverDepth` 1-19) and **washout
width = feet of missing roadbed** (`RiverWidth` 100-1499), plus weather. The menu is still built
dynamically from `RiverCrossChoice`, so the numbering shifts by which options the location
allows — **all 4–6 choices preserved:**

| # | Original | 2028 reskin | Odds (UNCHANGED) | Real basis |
|---|---|---|---|---|
| Ford | attempt to ford | **"GUN IT THROUGH THE HIGH WATER"** — free, fast | depth >3 ft past half-width → guaranteed `VehicleWashOut`; `CrossingResult` still 50% `StuckInMud` | Helene's I-40 collapse |
| Float | caulk & float | **"SEAL THE DOORS, TAKE THE WASHED-OUT SHOULDER DETOUR"** — free, DIY | depth >5 ft + 50% past half-width → `VehicleFloods` (drowned supplies + `ReduceMileage`) | Francine flooding I-10 |
| Ferry | take a ferry | **"PAY FOR THE NATIONAL GUARD HIGH-WATER CONVOY / CALTRANS ARMED RESUPPLY RUN"** — price feels like a surprise ambulance bill | `FerryCost $3-7`, `delay 1-9 days` via Resting; `>=` affordability; deduct in `CrossingTick` | Caltrans convoy runs on slid-out Hwy 1 |
| Guide | hire an Indian | **"HIRE A SOVEREIGN-CITIZEN LOCAL WHO 'ISN'T DRIVING, HE'S TRAVELING'"** — paid in **3-7 crates of MLM leggings** (the clothing slot), the only barter he takes | `HasEnoughClothingToTrade` gate + clothing deduction unchanged | Volusia County "I'm traveling not driving" stop |
| Wait | wait for conditions | **"WAIT FOR THE WATER TO RECEDE / FEMA TO REOPEN"** | `DaysToRest=1` → Resting (events still tick) | CA atmospheric-river closures |
| Info | get more information | **"ASK A PASSER-BY WHY THE INTERSTATE IS IN THE RIVER"** → `FordRiverHelp` becomes the deadpan Advice hook | — | the passer-by narrator device |

---

## 5. The route (Florida → Seattle)

Replaces `TrailRegistry.OregonTrail` with a `Location[]` of the same types. Each `[Type]` keeps
its original behavior (Settlement = store+chat, Landmark = gawk-only, RiverCrossing = §4, Fork =
`InsertLocation` skip-choices, Toll = price gate). Start: **Cape Coral, FL**.

1. **I-40 Pigeon River Gorge Washout** *(RiverCrossing)* — Helene tore ~4 mi of I-40 into the river; repairs to ~2028.
2. **Buc-ee's, Sevierville TN** *(Settlement)* — world's largest c-store; ammo by the flour.
3. **Touchdown Jesus, Monroe OH** *(Landmark)* — 62-ft Styrofoam Jesus struck by lightning & burned.
4. **Wall Drug, SD** *(Settlement)* — free-ice-water, 3,000-billboard trading post.
5. **Carhenge, Alliance NE** *(Landmark)* — 39 junk cars as Stonehenge.
6. **The I-44 Texas Detour** *(ForkInRoad)* — skip-choices: Big Texan Steak Ranch (Settlement; 72oz = food-sweep) / Cadillac Ranch (Landmark).
7. **Great Salt Lake Causeway Flood Crossing** *(RiverCrossing)* — desert highway under atmospheric-river flooding (Dry climate).
8. **Iowa State Fair Butter Cow** *(Landmark)* — 600-lb butter sculpture.
9. **Open-Carry Walmart, Springfield MO** *(Settlement)* — AR-15 "civics test" restock before the mountains.
10. **Columbia/Snake "Sovereign Citizen" Crossing** *(RiverCrossing)* — the guide-for-hire crossing, paid in leggings.
11. **Portland, OR** *(Settlement)* — Proud Boys vs antifascist brawls as police stand back (Polar climate).
12. **The Cascades — I-90 vs Highway 1 Fork** *(ForkInRoad)* — skip-choices: Tacoma "No Kings" rally town (Settlement) / nested Gorge fork → Columbia I-5 bridge (RiverCrossing) + **I-405 Express Toll Lanes** (TollRoad).
13. **Seattle, WA** *(Settlement, destination)* — end of trail.

> **Refinement note:** the satirical "greatest hits" ordering above is **not geographically
> linear** (TN→OH→SD→NE→TX→UT…). For plausibility, final ordering should trace a real arc
> (I-75 → I-40 → I-80 → I-90/I-84) and relocate set-pieces to states they'd actually pass. The
> *count and types* of locations are what matter mechanically; exact ordering is free tuning.

---

## 6. New encounters (the passer-by-explains-the-cause device)

Each maps onto an existing event/prefab. **The `causeFromPasserby` line is the joke** — it is
surfaced via the Advice system as a stranger's calm explanation. All are real, sourced (App. A).

| Encounter | Maps to | Passer-by's deadpan cause | Effect |
|---|---|---|---|
| **Florida Man at the Drive-Thru** | Wild / `ItemDestroyer` | *"Fella chucked a live gator he found on the median through the Wendy's window. Gator's fine."* | supplies scattered; small bite (Injure) chance |
| **The 80-yr-old Japanese Cyclist (Eiichi-san)** | Wild / `IndiansHelp` | *"Rode all the way from San Diego, sunburned, only words he had were 'Please. Rest. Water.' Fed him a week."* | +14 Food + morale |
| **The SubwaySean Sandwich Walker** | Wild / `ItemCreator` | *"Streamer pushed a stroller 3,500 miles eatin only Subway. They built him a solar stroller."* | gifts food / small cash |
| **Wrong Driveway** | Person / companion-kill | *"They pulled in to turn around. Homeowner fired twice. Round here they call it standing your ground."* | companion gravely Injured/killed |
| **Ring the Wrong Doorbell** | Person / heavy Damage | *"Kid rang the wrong bell pickin up his brothers. Shot through the glass. He lived."* | survivable heavy Damage |
| **Highway Patrol Forfeiture Stop** | Wild / `ItemDestroyer` (bandits) | *"Dog 'alerted,' so they took the cash — said the money was the suspect. No charges."* | seizes Cash/supplies; 3-15 "ammo"; resist → `TryKill` |
| **The Prosperity-Gospel Preacher** | Wild / `FoodDestroyer` | *"Says commercial flights are full of demons, that's why he needs a third jet. Sow a seed."* | drains cash/supplies for nothing |
| **The MLM Hun** | Wild / Trade | *"Mortgaged the house for ten crates of leggings. Now she needs YOU to buy in."* | bad-odds swap; stocks the leggings you pay the guide with |
| **To The Moon (Diamond Hands)** | Wild / gamble | *"Whole crowd's dumpin savings into a dyin mall store to stick it to the hedge funds."* | coin-flip: moon (big gain) or wipeout |
| **No Kings Wagon Train** | Wild / `LoseTime` | *"Couple million folks marchin. Point is we don't have a king — been sayin it 250 years."* | lose days, no damage |
| **Rolling Coal** | Wild / hostile-pass | *"He paid to make that truck dump black smoke on command. Did it at the cyclists."* | morale/health hit + minor item loss |
| **Black Friday Stampede** | Animal / `BuffaloStampede` | *"Doors opened and the crowd went over the top of him for a discount TV."* | crush Injures/kills a member |
| **IShowSpeed Street Takeover** | Wild / crowd-chaos | *"One streamer walks through and suddenly it's donuts, lowriders, fireworks."* | stalls wagon, minor fire/injury, lose time |
| **Emotional-Support Alligator Wanders Off** | Vehicle / `OxenWanderOff` | *"CERTIFIED emotional-support gator, turned away from a Phillies game. Slipped its pen."* | companion animal / repo'd SUV goes missing |
| **The Roadside Commune Pitch** | Wild / time-sink | *"Walked three miles 'fore a commune fella tried to recruit me. Just kept walkin."* | harmless unless you accept |

---

## 7. Causes of death (was: dysentery, cholera…)

Reskinned `Render` strings on the existing death/infect/injure prefabs — **no mechanic change**:

- You have died of **diabetic ketoacidosis**. The insulin GoFundMe came up **$50 short**.
- You have died of **heat exhaustion**. It was, the locals noted, a dry heat — **110°F for the 31st straight day**.
- You have **frozen to death indoors**. The grid failed; the lights were off for four days.
- You have been killed by a **falling celebratory bullet** fired into the air a mile away at midnight.
- You have been **shot for ringing the wrong doorbell**.
- You have been **shot for pulling into the wrong driveway** while turning around.
- You have died of **respiratory failure**. The sky was orange and the **AQI hit 484**.
- You have been **trampled in a Black Friday door-buster stampede**.
- You have been **struck by a car** walking the shoulder of a 55-mph US highway. There was no sidewalk.
- You have been **incinerated** — the 62-ft Styrofoam Jesus was struck by lightning despite its lightning rods.
- You have died of a **"Don't Tread on Me" rattlesnake** bite.
- You have died of **cancer**. Only **11% of the GoFundMes** reach goal in time.
- You have died of a **$9,076 surprise ambulance bill** — survivable injury, fatal invoice.
- You have died of an **untreated infection**. The nearest in-network hospital was three states away.

---

## 8. Implementation map (where the code actually changes)

Mostly **string/flavor edits**, plus a small number of new event classes and one wiring fix.
Per the repo's verified architecture (`CLAUDE.md`):

- **New events = new classes only.** Add a class under `src/Event/<Category>/`, derive from
  `EventProduct` or a Prefab base, tag `[DirectorEvent(EventCategory.X)]`; the reflection
  factory discovers it. Put setup in `OnEventCreate()` (ctor does **not** run —
  `GetUninitializedObject`); `EventKey` is by **class name only**, so names must be unique;
  `Render` must return non-empty text.
- **Route:** rewrite `src/Module/Trail/TrailRegistry.cs` (§5). `TrailModule` ctor still points
  at `TrailRegistry.OregonTrail` (rename the property or repoint the ctor).
- **Items/store/professions:** edit `[Description]`/label strings in `src/Entity/Item/*.cs`,
  `Entities.cs`, `Profession.cs`, `Store/*`. **No numeric edits** — see §3.
- **Crossing/toll/scoring strings:** `src/Window/Travel/RiverCrossing/*`,
  `Toll/*`, `src/Window/GameOver/FinalPoints.cs` (rating labels only — **do not touch the
  profession `switch`**).
- **Advice = the narrator.** `src/Window/Travel/TalkToPeople/AdviceRegistry.cs` holds the
  `Landmark`/`Settlement` advice buckets — rewrite these to deliver the passer-by "cause" lines.
  This is the highest-leverage file for the whole satire.

### 🔧 Required wiring fix (do not skip)

> The base game **never fires the `Wild` or `Animal` event categories** — `TriggerEventByType`
> is only called for `Weather` (`LocationWeather.cs`), `Person` (`Person.cs`), `Vehicle`
> (`Vehicle.cs`), and `RiverCross` (`CrossingTick.cs`). **Most of the §6 encounters are tagged
> `Wild`/`Animal`, so as-is they will register but never appear.** To make roadside America
> happen, add a `TriggerEventByType(this, EventCategory.Wild)` (and `.Animal`) call into the
> per-day travel tick (e.g. `TrailModule.OnTick` / `Person.OnTick` or
> `Window/Travel/Command/ContinueOnTrail.cs`). This is the one genuinely *new mechanical wire*
> the mod needs. (Tag rare set-pieces `EventExecution.ManualOnly` and fire them at specific
> locations instead, if you'd rather script them.)

---

## 9. Sanctioned non-string changes (everything else is out of scope)

These are the **only** mechanical edits the mod is allowed; all carry an open question (§10):

1. **Wire up `Wild`/`Animal` triggers** (above) — without it, half the content is dead.
2. **Climate enum:** the satire wants 5+ regimes (hurricane / heat-dome / atmospheric river /
   wildfire smoke / ice-storm) but `Climate` has **4 values** (Moderate/Continental/Dry/Polar).
   Either *overload the 4 by region* (zero code) or *extend the enum* + the `LocationWeather`
   switch + `ClimateData` (more faithful). **Prefer overloading** to honor the "same odds" rule.
3. *(Optional)* **Event fire-rate:** the `Random.Next(100)==0` (~1%/call) roll is sparse; many
   best bits may never surface. Raising it is a tuning, not a mechanic, change — gated on §10.

Anything not in this list (damage values, costs, thresholds, yields) **stays byte-identical.**

---

## 10. Open design questions

1. **Guide currency** — pay the river guide in the reskinned "clothes" slot as MLM leggings, or
   retire the clothes-as-payment quirk and just take cash?
2. **Climate** — overload the 4 enums by region, or extend the enum (+switch)? (Spec leans overload.)
3. **Fire rate** — keep 1%/call byte-identical (risking unseen content), or raise frequency?
4. **Tone guardrail** — wrong-driveway/doorbell shootings as random deaths: keep strictly
   deadpan & non-partisan by sourcing every line from the passer-by's *normalizing* voice only.
   Confirm this is the bar.
5. **Hunting vs Stampede** — the "food sweep" minigame and the Black Friday Stampede both lean on
   crowds; merge them, or keep minigame + event distinct?
6. **Destination payoff** — does Seattle get an ironic twist (also unaffordable / on fire), or
   stay a clean "you made it" to preserve the original win-state?
7. **Persistence** — the GoFundMe-headstone bit lands far harder if graves persist across runs,
   but tombstone (and high-score) save/load are **unfinished `// TODO: JSON` stubs** in the base
   game. Finally wire the JSON save, or keep in-memory like the original?
8. **Forfeiture & fighting back** — civil forfeiture as the "bandits" reskin: keep the
   ammo-spend "resist" mechanic even though resisting realistically makes outcomes worse?

---

## Appendix A — Real-world sourcing

Every beat above is drawn from a real, reported event. (Fact-checked via live web search; one
representative source each.)

**Roadside attractions:** Cadillac Ranch (Texas Standard); Carhenge (Wikipedia); Wall Drug
(InsideHook); Buc-ee's world's-largest, 120 pumps (CSP Daily News); Big Texan 72oz (Wikipedia);
Touchdown Jesus lightning fire (NBC News).
**Crime / guns / wildlife:** alligator through Wendy's window (TIME, 2015); emotional-support
gator "Wally" missing (CNN, 2024); Ralph Yarl wrong-doorbell shooting (CNN, 2023); wrong-driveway
fatal shooting, Hebron NY (CNN, 2023); child killed by falling NYE bullet (KRIS 6, 2022).
**Open carry / sovereign citizens / forfeiture:** AR-15 Walmart "2A test," Springfield MO (NPR,
2019); FL 26th permitless-carry state (The Trace, 2023); Stephen Lara's $87k seized (Reason,
2021); OK $100k farmland cash taken (NBC News, 2021); "I'm traveling not driving," Volusia County
(FOX 35, 2023).
**Climate:** Phoenix 31 days ≥110°F (CNBC, 2023); I-40 into the Pigeon River, fix to 2028 (ABC11,
2024); Winter Storm Uri / Texas grid (Wikipedia, 2021); CA atmospheric rivers, Hwy 1 (CNN, 2023);
NYC orange wildfire smoke, AQI 484 (CBS, 2023); Francine floods I-10 (NOLA.com, 2024).
**Healthcare / money:** Shane Boyle insulin GoFundMe, died $50 short (Snopes, 2017); $9,076
ambulance bill (WUSA9, ~2022); televangelist's "demons on planes" jet defense (Washington Post,
2019); LuLaRoe "cult" + $4M WA settlement (Rolling Stone, 2021); GameStop "diamond hands" (NPR,
2021); 91k cancer GoFundMes, ~11% funded (American Cancer Society).
**Protests / culture war:** "No Kings" nationwide marches (PBS NewsHour, 2025); armed protesters
in Michigan Capitol (Detroit News, 2020); ~22k armed at Virginia Capitol (NPR, 2020); Portland
Proud Boys vs antifa (OPB, 2021); book bans +200% (PEN America, 2023-24); Gadsden-flag patch
removal (CPR News, 2023).
**Consumer excess:** Big Texan 72oz (Texas Monthly); state-fair deep-fry list (Dallas Observer);
600-lb butter cow (Smithsonian); drive-thru open-casket viewings (TIME, 2014); Black Friday
trampling death (Seattle Times, 2008); "rolling coal" DOJ suit (CNBC, 2023).
**Streamers / cross-country walkers:** SubwaySean's 3,500-mi Subway-only walk (WWLP, 2022);
commune recruitment mid-stream (Sportskeeda, 2022); 80-yr-old Japanese cyclist hosted in Alabama
(NextShark, 2024); "Barefoot Dutchman" Guinness record (NL Times, 2024); faith-walk streamer hit
by car on US-40 (NBC10, 2026); IShowSpeed street-takeover hijack (Dexerto, 2025-26).

*Full URLs preserved in the workflow research output; available on request.*
