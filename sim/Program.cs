// Headless economic/survival simulator for "The Asphalt Trail" (2028 rebalance).
//
// This is a faithful PORT (not the live game) of the economy-relevant systems, transcribed
// directly from the game source so we can run thousands of playthroughs without a TTY:
//   - Purchase phase (2028 prices, profession budgets, item max/min quantities)
//   - Daily mileage formula (Vehicle.RandomMileage) incl. day-to-day compounding + 50% halving
//   - Food burn = ration * livingCount; starvation damage
//   - Ration/clothes-driven illness (Person.OnTick / CheckIllness) at the rebalanced category odds
//   - Pace penalty (Strenuous/Grueling)
//   - Vehicle events 4%/day: BrokenVehiclePart (strand w/o spare), OxenDied, item-destroyers, lose-time
//   - Dynamic fuel-price curve (FuelPricing.cs): store price = $25 base scaled 1.0x start -> 0.7x mid
//     -> 2.0x end by journey progress. This ONLY prices fuel PAID for (mid-trip refuel + DoorDash burn);
//     the daily mileage formula is left calibrated to $25/can and is NOT touched.
//   - DoorDash gig mini-game (DoorDashManager.cs + DeliveryOffer.cs) behind --doordash <N>: N gig shifts
//     spread across the journey. Each shift earns cash (insulting base pay + realized tips w/ tip-bait,
//     unicorn and cancel skews) while burning the party's own gas cans + spare tires and costing a full
//     day of food/health. Net is booked honestly (gross - fuel@curve - tires@$200). See RunDoorDashShift.
//   - End-game scoring (FinalPoints) incl. profession multiplier and rating bands
//
// Faithfully MODELLED approximations are tagged [APPROX]; Weather/Wild events are treated as
// negligible (flavor/time only) and noted in the report. Trail length = N segments ~ U[32,164).
//
// Usage:
//   asphaltsim --profession Farmer --gas 10 --food 300 --ration 1 --pace 1 --trials 4000 --seed 1
//   asphaltsim --profession Farmer --gas 12 --food 300 --tires 3 --doordash 3 --trials 4000 --seed 1
//   asphaltsim --profession Farmer --vehicle Minivan --gas 10 --food 300 --trials 4000 --seed 1
// Emits a JSON summary on stdout. When --doordash is 0 (default) and --vehicle is Minivan (default,
// the pre-choice baseline: cost/speed/fuel/cargo/party numerically identical to the original hardcoded
// values), behavior + output are byte-for-byte identical to the pre-vehicle-choice sim.
// --vehicle {Minivan,PickupCamper,HybridCrossover,ElectricHatchback} mirrors VehicleModels.Get: subtracts
// Cost from starting cash (floored at $0, same as Vehicle.Balance's setter), scales the mileage formula by
// SpeedMultiplier/FuelEfficiencyMultiplier (mirrors Vehicle.RandomMileage/PaceMultiplier), clamps the
// cargo-weight purchase phase (clothes + food, the only two items with nonzero SimItem.Weight) to
// CargoCapacity, and caps party size at MaxPartySize (mirrors Vehicle.MaxPartySize, itself bounded by
// GameSimulationApp.MAXPLAYERS = 4).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

internal static class Program
{
    // ---- 2028 rebalanced prices (from Resources.cs / Parts.cs) ----
    const double PGas = 25, PTire = 200, PAlt = 300, PTrans = 1200, PCloth = 75, PAmmo = 30, PFood = 1.50;
    // ---- max quantities (inventory caps) ----
    const int MaxGas = 20, MaxTire = 3, MaxAlt = 3, MaxTrans = 3, MaxCloth = 50, MaxAmmo = 99, MaxFood = 2000;
    const int AmmoMin = 20;

    // ---- scoring points-per/awarded (from SimItem ctor args) ----
    // gas: awarded 4 / per 1 ; spares: awarded 2 / per 1 ; clothes: awarded 2 / per 1
    // ammo: awarded 1 / per 50 ; food: awarded 1 / per 25 ; cash: awarded 1 / per 5

    const int MaxPlayers = 4;         // GameSimulationApp.MAXPLAYERS
    const int SegMin = 32, SegMax = 164;

    // ---- vehicle catalog (from VehicleModels.Get) ----
    // Minivan is the numeric baseline (Speed 1.0, FuelEff 1.0) and, at $1200, the post-fix affordable
    // option for every profession (Farmer's $2000 budget included).
    struct VehicleModel { public string Name; public float Cost; public double Speed, FuelEff; public int Cargo, MaxParty; }
    static VehicleModel VehicleData(string v) => v switch
    {
        "Minivan" => new VehicleModel { Name = "Beige Minivan", Cost = 1200f, Speed = 1.0, FuelEff = 1.0, Cargo = 300, MaxParty = 4 },
        "PickupCamper" => new VehicleModel { Name = "Lifted Pickup with Camper Shell", Cost = 6500f, Speed = 0.9, FuelEff = 0.7, Cargo = 500, MaxParty = 4 },
        "HybridCrossover" => new VehicleModel { Name = "Hybrid Crossover SUV", Cost = 4500f, Speed = 1.1, FuelEff = 1.4, Cargo = 200, MaxParty = 3 },
        "ElectricHatchback" => new VehicleModel { Name = "Secondhand EV Hatchback", Cost = 5000f, Speed = 1.2, FuelEff = 1.6, Cargo = 150, MaxParty = 3 },
        _ => throw new ArgumentException($"Unknown --vehicle '{v}'; expected Minivan, PickupCamper, HybridCrossover, or ElectricHatchback")
    };

    // ---- DoorDash gig mini-game constants (mirrors DoorDashManager.cs / DeliveryOffer.cs) ----
    const int SHIFTTIME = 30;         // shift ticks per day of Dashing
    const int MILESPERCAN = 30;       // total miles (paid+deadhead) that burn one gas can
    const int WEARPERTIRE = 45;       // total miles that wear out one spare tire
    const double ACCEPTRATE_FLOOR = 0.4;
    const double CANCEL_CHANCE = 0.08;
    // Driver accept policy (see RunDoorDashShift): a rational Dasher only sees BasePay, EstimatedTip and
    // PaidDistance (deadhead is hidden), so we accept when shown value density clears this $/paid-mile bar.
    // 1.75 is the common "$2/mi-ish" delivery rule of thumb; it settles acceptance ~50-65% at good quality
    // and makes rejecting garbage correct while the acceptance-rate spiral still punishes pickiness.
    const double ACCEPT_THRESHOLD_PER_MILE = 1.75;

    // ---- fuel price curve (mirrors FuelPricing.cs) ----
    // Multiplier by journey progress p = segIdx / (segments-1): 1.0x at start, 0.7x at mid, 2.0x at end.
    static double FuelMult(int segIdx, int segments)
    {
        if (segments <= 1) return 1.0;
        double p = (double)segIdx / (segments - 1);
        if (p < 0) p = 0; else if (p > 1) p = 1;
        if (p <= 0.5) return 1.0 + (0.7 - 1.0) * (p / 0.5);
        return 0.7 + (2.0 - 0.7) * ((p - 0.5) / 0.5);
    }
    // Per-can store price at a given trail progress (base $25 scaled by the curve). Used ONLY to value fuel
    // the DoorDash driver burns; the mileage formula keeps its $25 calibration (costAnimals below).
    static double FuelPrice(int segIdx, int segments) => Math.Round(PGas * FuelMult(segIdx, segments), 2);

    static int StartMoney(string prof) => prof switch
    {
        "Banker" => 8000, "Carpenter" => 4000, "Farmer" => 2000, _ => throw new ArgumentException("prof")
    };
    static int ProfMult(string prof) => prof switch
    {
        "Banker" => 1, "Carpenter" => 2, "Farmer" => 3, _ => throw new ArgumentException("prof")
    };

    static int Main(string[] argv)
    {
        var a = ParseArgs(argv);
        string prof = a.GetValueOrDefault("profession", "Farmer");
        string vehicleChoice = a.GetValueOrDefault("vehicle", "Minivan");
        var veh = VehicleData(vehicleChoice);
        int partySize = Math.Min(veh.MaxParty, MaxPlayers);
        int gas = I(a, "gas", 8), food = I(a, "food", 200), tires = I(a, "tires", 0),
            alts = I(a, "alternators", 0), trans = I(a, "transmissions", 0),
            cloth = I(a, "clothes", 0), ammo = I(a, "ammo", 0),
            ration = I(a, "ration", 1), pace = I(a, "pace", 1),
            trials = I(a, "trials", 4000), seed = I(a, "seed", 1),
            segments = I(a, "segments", 16),
            doordash = I(a, "doordash", 0);
        if (doordash < 0) doordash = 0;

        // ---- vehicle purchase: subtract Cost from starting cash, flooring at $0 (mirrors
        // GameSimulationApp.SetStartInfo -> Vehicle.Balance's setter) ----
        int rawBudget = (int)(StartMoney(prof) - veh.Cost);
        bool vehicleFloored = rawBudget <= 0;
        int budget = vehicleFloored ? 0 : rawBudget;

        // ---- validate purchase ----
        var clampNotes = new List<string>();
        if (gas > MaxGas) { clampNotes.Add($"gas {gas}->{MaxGas}"); gas = MaxGas; }
        if (tires > MaxTire) { clampNotes.Add($"tires->{MaxTire}"); tires = MaxTire; }
        if (alts > MaxAlt) { clampNotes.Add($"alts->{MaxAlt}"); alts = MaxAlt; }
        if (trans > MaxTrans) { clampNotes.Add($"trans->{MaxTrans}"); trans = MaxTrans; }
        if (cloth > MaxCloth) { clampNotes.Add($"cloth->{MaxCloth}"); cloth = MaxCloth; }
        if (ammo > MaxAmmo) { clampNotes.Add($"ammo->{MaxAmmo}"); ammo = MaxAmmo; }
        if (food > MaxFood) { clampNotes.Add($"food->{MaxFood}"); food = MaxFood; }
        if (ammo > 0 && ammo < AmmoMin) { clampNotes.Add($"ammo {ammo}->{AmmoMin} (min buy)"); ammo = AmmoMin; }

        // ---- cargo capacity clamp: clothes + food are the only two items with nonzero SimItem.Weight
        // (both weight 1/unit), so they're the only purchases CargoCapacity actually constrains. ----
        if (cloth > veh.Cargo) { clampNotes.Add($"cloth {cloth}->{veh.Cargo} (cargo cap)"); cloth = veh.Cargo; }
        int remainingCargo = veh.Cargo - cloth;
        if (food > remainingCargo)
        {
            clampNotes.Add($"food {food}->{Math.Max(0, remainingCargo)} (cargo cap)");
            food = Math.Max(0, remainingCargo);
        }

        double cost = gas * PGas + tires * PTire + alts * PAlt + trans * PTrans
                      + cloth * PCloth + ammo * PAmmo + food * PFood;
        bool affordable = cost <= budget;
        int leftoverCash = (int)Math.Max(0, budget - cost);

        if (!affordable)
        {
            Console.WriteLine(Json(new (string, object)[]
            {
                ("profession", prof), ("vehicle", veh.Name), ("vehicle_cost", veh.Cost),
                ("vehicle_floored_cash", vehicleFloored),
                ("affordable", false), ("cost", Math.Round(cost)),
                ("budget", budget), ("over_by", Math.Round(cost - budget))
            }));
            return 0;
        }

        // ---- run trials ----
        int wins = 0, deathLoss = 0, strandLoss = 0, starveLoss = 0, timeout = 0;
        var days = new List<int>(); var scores = new List<int>(); var endCash = new List<int>();
        var ratings = new Dictionary<string, int> { ["Greenhorn"] = 0, ["Adventurer"] = 0, ["TrailGuide"] = 0 };
        int fullPartyAlive = 0;
        var livingAtEnd = new List<int>();

        // DoorDash telemetry (only populated when doordash > 0).
        var ddShiftNets = new List<double>();   // honest net per shift, all trials
        var ddShiftGross = new List<double>();   // gross per shift, all trials
        var ddTotals = new long[3];              // [0]=cans burned, [1]=tires worn, [2]=shifts run

        for (int t = 0; t < trials; t++)
        {
            var r = new Rng(seed * 100003 + t);
            var res = Simulate(r, prof, gas, food, tires, alts, trans, cloth, ammo, ration, pace, leftoverCash, segments,
                doordash, ddShiftNets, ddShiftGross, ddTotals, partySize, veh.Speed, veh.FuelEff);
            days.Add(res.Days);
            if (res.Outcome == Outcome.Win)
            {
                wins++; scores.Add(res.Score); endCash.Add(res.EndCash); ratings[res.Rating]++;
                livingAtEnd.Add(res.Living);
                if (res.Living == partySize) fullPartyAlive++;
            }
            else
            {
                scores.Add(0);
                switch (res.Outcome)
                {
                    case Outcome.Death: deathLoss++; break;
                    case Outcome.Stranded: strandLoss++; break;
                    case Outcome.Starved: starveLoss++; break;
                    case Outcome.Timeout: timeout++; break;
                }
            }
        }

        double Pct(int n) => Math.Round(100.0 * n / trials, 1);
        var fields = new List<(string, object)>
        {
            ("profession", prof),
            ("vehicle", veh.Name), ("vehicle_cost", veh.Cost), ("vehicle_floored_cash", vehicleFloored),
            ("party_size", partySize), ("speed_mult", veh.Speed), ("fuel_eff_mult", veh.FuelEff), ("cargo_cap", veh.Cargo),
            ("strategy", $"gas{gas} food{food} tire{tires} alt{alts} trans{trans} cloth{cloth} ammo{ammo} ration{ration} pace{pace}"),
            ("budget", budget), ("spend", (int)Math.Round(cost)), ("leftover_cash", leftoverCash),
            ("trials", trials),
            ("win_pct", Pct(wins)),
            ("loss_death_pct", Pct(deathLoss)),
            ("loss_starved_pct", Pct(starveLoss)),
            ("loss_stranded_pct", Pct(strandLoss)),
            ("loss_timeout_pct", Pct(timeout)),
            ("median_days", Median(days)),
            ("median_score", wins > 0 ? Median(scores.Where((s, i) => true).ToList()) : 0),
            ("median_score_winners", wins > 0 ? Median(scores.Where(s => s > 0).ToList()) : 0),
            ("median_endcash_winners", wins > 0 ? Median(endCash) : 0),
            ("full_party_alive_pct_of_wins", wins > 0 ? Math.Round(100.0 * fullPartyAlive / wins, 1) : 0),
            ("rating_greenhorn", ratings["Greenhorn"]),
            ("rating_adventurer", ratings["Adventurer"]),
            ("rating_trailguide", ratings["TrailGuide"]),
        };
        // DoorDash fields are emitted ONLY when gigging is enabled, so --doordash 0 output is byte-identical
        // to the pre-DoorDash sim.
        if (doordash > 0)
        {
            fields.Add(("doordash_shifts", doordash));                                  // shifts scheduled per trial
            fields.Add(("doordash_shifts_run_mean", Math.Round((double)ddTotals[2] / trials, 2))); // actually run (skips when out of gas)
            fields.Add(("doordash_gross", MedianD(ddShiftGross)));                      // median gross $/shift
            fields.Add(("doordash_net_median", MedianD(ddShiftNets)));                  // median honest net $/shift (gross - fuel@curve - tires@$200)
            fields.Add(("doordash_net_mean", ddShiftNets.Count > 0 ? Math.Round(ddShiftNets.Average(), 2) : 0)); // mean net $/shift (rare $200 tire hits drag this << median)
            fields.Add(("doordash_fuel_cans_used", Math.Round((double)ddTotals[0] / trials, 2)));  // mean cans burned per trial
            fields.Add(("doordash_tires_used", Math.Round((double)ddTotals[1] / trials, 2)));      // mean spare tires burned per trial
        }
        if (clampNotes.Count > 0) fields.Add(("clamped", string.Join("; ", clampNotes)));
        Console.WriteLine(Json(fields));
        return 0;
    }

    enum Outcome { Win, Death, Stranded, Starved, Timeout }
    struct Result { public Outcome Outcome; public int Days, Score, EndCash, Living; public string Rating; }

    // ddTotals accumulates across ALL trials: [0]=gas cans burned, [1]=tires worn, [2]=shifts actually run.
    static Result Simulate(Rng r, string prof,
        int gas, int food, int tires, int alts, int trans, int cloth, int ammo,
        int ration, int pace, int cash, int segments,
        int doordash, List<double> ddShiftNets, List<double> ddShiftGross, long[] ddTotals,
        int partySize, double speedMult, double fuelEffMult)
    {
        // ---- DoorDash schedule: spread N shifts across intermediate settlements so the fuel-price curve and
        // acceptance-rate spiral actually vary across the journey. Shift k fires when we ARRIVE at segIdx
        // round((k+1)*segments/(N+1)), clamped to [1, segments-1]. (No RNG; a no-op when doordash == 0.) ----
        int[] shiftAtSeg = new int[segments + 1];
        if (doordash > 0)
            for (int k = 0; k < doordash; k++)
            {
                int seg = (int)Math.Round((k + 1) * (double)segments / (doordash + 1));
                if (seg < 1) seg = 1; else if (seg > segments - 1) seg = segments - 1;
                shiftAtSeg[seg]++;
            }

        // ---- party ----
        var hp = new int[partySize];
        var dead = new bool[partySize];
        var injured = new bool[partySize];
        var infected = new bool[partySize];
        for (int i = 0; i < partySize; i++) hp[i] = 500;
        int Living() { int c = 0; for (int i = 0; i < partySize; i++) if (!dead[i]) c++; return c; }

        // ---- trail: distance to traverse = sum of per-segment U[SegMin,SegMax) ----
        // (each location's TotalDistance = Random.Next(32,164); you arrive when cumulative >= seg)
        double mileage = 1;           // Vehicle.Mileage, persists & compounds
        double costAnimals = gas * PGas;
        double paceMult = pace == 2 ? 1.3 : pace == 3 ? 1.6 : 1.0;

        int segIdx = 0;
        int segTarget = r.Next(SegMin, SegMax);
        int day = 0;
        const int DayCap = 2000;

        while (day < DayCap)
        {
            day++;
            int living = Living();
            if (living == 0) return Lose(Outcome.Death, day);

            // ----- per-passenger tick (Person.OnTick faithful order) -----
            for (int i = 0; i < partySize; i++)
            {
                if (dead[i]) continue;

                // ration-driven illness exposure
                if (ration == 3) CheckIllness(r, hp, dead, injured, infected, i, ration, partySize, ref mileage);
                else if (ration == 2 && r.Bool()) CheckIllness(r, hp, dead, injured, infected, i, ration, partySize, ref mileage);

                // clothes branch (Person.OnTick), FIXED: adequate clothing now LOWERS illness exposure.
                // costClothes = crates*$75; Randomizer.Next() is [0,60) so warm threshold ~ $140 (~2 crates),
                // reliably warm ~ 4 crates ($300). Warm -> 25% branch; underdressed -> always CheckIllness.
                double costClothes = cloth * PCloth;
                if (costClothes > 22 + 4 * r.Next(0, 60))
                {
                    if (r.Bool() && r.Bool()) CheckIllness(r, hp, dead, injured, infected, i, ration, partySize, ref mileage);
                }
                else
                    CheckIllness(r, hp, dead, injured, infected, i, ration, partySize, ref mileage);

                // pace penalty (moving travel day only)
                if (pace == 2) Damage(r, hp, dead, injured, infected, i, 2, 6);
                else if (pace == 3) { Damage(r, hp, dead, injured, infected, i, 6, 14); CheckIllness(r, hp, dead, injured, infected, i, ration, partySize, ref mileage); }
                if (dead[i] && i == 0) return Lose(Outcome.Death, day);

                // food consumption — FAITHFUL: the game calls ConsumeFood once PER living passenger, and
                // each call reduces food by mult*livingCount, so whole-party daily burn is quadratic:
                // mult * count^2 (Filling 4 people = 3*4*4 = 48 lb/day). INVERTED mult (Filling=3..BareBones=1).
                if (food > 0)
                {
                    food = Math.Max(0, food - (4 - ration) * Living());
                    Heal(r, hp, dead, i);
                }
                else
                {
                    Damage(r, hp, dead, injured, infected, i, 10, 50);
                    if (dead[i] && i == 0) return Lose(Outcome.Starved, day);
                }
            }
            if (Living() == 0) return Lose(Outcome.Starved, day);
            if (dead[0]) return Lose(Outcome.Death, day);

            // ----- vehicle moves -----
            // RandomMileage: compounding base + gas bonus (scaled by FuelEfficiencyMultiplier) + jitter
            double gasMiles = (costAnimals - 137.5) / 3.125 * fuelEffMult;
            double totalMiles = mileage + gasMiles + 10 * r.NextDouble();
            mileage = Math.Abs((int)totalMiles);
            // PaceMultiplier = paceFactor * SpeedMultiplier; applied whenever pace isn't Steady, or the
            // vehicle's SpeedMultiplier isn't the 1.0 baseline (mirrors Vehicle.TickTravel).
            if (pace != 1 || !speedMult.Equals(1.0)) mileage = (int)(mileage * paceMult * speedMult);
            if (r.Bool() && mileage > 0) mileage = (int)(mileage / 2);

            // vehicle event 4%/day
            if (r.Next(100) < 4)
            {
                var v = VehicleEvent(r, ref gas, ref tires, ref alts, ref trans, ref cloth, ref ammo, ref food);
                if (v == VehOut.Stranded) return Lose(Outcome.Stranded, day);
            }
            if (gas <= 0) return Lose(Outcome.Stranded, day); // no fuel -> disabled

            if (mileage <= 0) mileage = 10;

            // advance trail
            segTarget -= (int)mileage;
            if (segTarget < 0)
            {
                segIdx++;
                if (segIdx >= segments)
                {
                    // arrived at Seattle -> WIN, tabulate score
                    return Win(prof, hp, dead, gas, tires, alts, trans, cloth, ammo, food, cash, day, partySize);
                }
                segTarget = r.Next(SegMin, SegMax);

                // DoorDash: run any shifts scheduled for this settlement (cities only in the real game).
                if (doordash > 0)
                    for (int s = 0; s < shiftAtSeg[segIdx]; s++)
                    {
                        if (gas <= 0) break;   // no fuel -> can't Dash (ShouldEndShift immediately)
                        var dd = RunDoorDashShift(r, segIdx, segments, ref gas, ref tires, ref cash,
                            hp, dead, injured, infected, ration, cloth, ref food, ref mileage, ref day,
                            ddShiftNets, ddShiftGross, ddTotals, partySize);
                        if (dd.HasValue) return Lose(dd.Value, day);
                    }
            }
        }
        return Lose(Outcome.Timeout, day);
    }

    // ---- DoorDash: run ONE shift (a full day). Mirrors DoorDashManager + DeliveryOffer. ----
    // Surfaces offers, a rational driver accepts/rejects them (ACCEPT_THRESHOLD_PER_MILE), burns the party's
    // OWN gas cans + spare tires on total (paid+deadhead) miles, and banks cash. Then the shift day passes:
    // party eats + health rolls (mirrors DoorDashResult.OnFormPostCreate -> TakeTurn(false)); no trail
    // movement, no travel pace penalty. Net is booked honestly: gross - fuel@curve-price - tires@$200.
    // Returns null if the leader survives the day, else the death Outcome.
    static Outcome? RunDoorDashShift(Rng r, int segIdx, int segments,
        ref int gas, ref int tires, ref int cash,
        int[] hp, bool[] dead, bool[] inj, bool[] inf, int ration, int cloth,
        ref int food, ref double mileage, ref int day,
        List<double> nets, List<double> gross, long[] totals, int partySize)
    {
        int secondsRemaining = SHIFTTIME;
        int milesDriven = 0, wearMiles = 0;
        int offersSeen = 0, accepted = 0, cansBurned = 0, tiresWorn = 0;
        double grossEarned = 0;

        // Gig loop: one iteration == one shift tick (DoorDashManager.OnTick). [APPROX] A rational driver
        // decides the instant an offer surfaces (never dithers), so offers never expire; the acceptance-rate
        // spiral is still driven by rejects (OfferQuality = 0.4 + 0.6*acceptRate), so pickiness is punished.
        while (secondsRemaining > 0 && gas > 0)
        {
            secondsRemaining--;

            // TrySurfaceOffer: offers do not arrive every tick (~50% NextBool).
            if (r.Bool()) continue;

            double acceptRate = offersSeen <= 0 ? 1.0 : accepted / (double)offersSeen;
            double quality = ACCEPTRATE_FLOOR + (1.0 - ACCEPTRATE_FLOOR) * acceptRate;
            offersSeen++;

            // DeliveryOffer ctor (cosmetic restaurant/dropoff draws + unused DecideTicksMax omitted).
            double basePay = 2.0 + r.NextDouble() * 1.5;
            double estTip = r.NextDouble() * 12.0 * (0.5 + 0.5 * quality);
            int paidDist = r.Next(2, 9);
            int deadhead = r.Next(1, 6) + (quality < 0.6 ? r.Next(0, 4) : 0);
            int waitTicks = r.Next(0, 5);
            int totalMiles = paidDist + deadhead;

            // Accept policy: the driver only sees base + estimated tip over the PAID distance (deadhead is
            // hidden), and accepts when that shown $/paid-mile clears the threshold — like a real Dasher.
            double shownDensity = (basePay + estTip) / Math.Max(1, paidDist);
            if (shownDensity < ACCEPT_THRESHOLD_PER_MILE)
                continue; // Reject: no earnings, acceptance rate drops -> worse future offers.

            // ---- Accept ----
            accepted++;
            milesDriven += totalMiles;
            wearMiles += totalMiles;
            secondsRemaining -= waitTicks;
            if (secondsRemaining < 0) secondsRemaining = 0;

            // Burn fuel + wear tires from the ACTUAL inventory (this is the whole tradeoff).
            while (milesDriven >= MILESPERCAN) { milesDriven -= MILESPERCAN; if (gas <= 0) break; gas--; cansBurned++; }
            while (wearMiles >= WEARPERTIRE) { wearMiles -= WEARPERTIRE; if (tires <= 0) break; tires--; tiresWorn++; }

            // Collect pay: 8% cancelled-after-pickup -> base pay only; else base + realized (usually-worse) tip.
            double pay = r.NextDouble() < CANCEL_CHANCE ? basePay : basePay + RealizedTip(r, estTip);
            grossEarned += pay;
            cash += (int)Math.Round(pay); // mirrors AddQuantity((int)Math.Round(pay))
        }

        // Honest net: fuel valued at the current curve price, tires at $200 flat.
        double net = grossEarned - cansBurned * FuelPrice(segIdx, segments) - tiresWorn * PTire;
        nets.Add(net);
        gross.Add(grossEarned);
        totals[0] += cansBurned; totals[1] += tiresWorn; totals[2] += 1;

        // ---- The shift consumes a full day: party eats + health rolls; no travel, no pace penalty. ----
        day++;
        int living = 0; for (int i = 0; i < partySize; i++) if (!dead[i]) living++;
        if (living == 0) return Outcome.Death;

        for (int i = 0; i < partySize; i++)
        {
            if (dead[i]) continue;

            if (ration == 3) CheckIllness(r, hp, dead, inj, inf, i, ration, partySize, ref mileage);
            else if (ration == 2 && r.Bool()) CheckIllness(r, hp, dead, inj, inf, i, ration, partySize, ref mileage);

            double costClothes = cloth * PCloth;
            if (costClothes > 22 + 4 * r.Next(0, 60))
            {
                if (r.Bool() && r.Bool()) CheckIllness(r, hp, dead, inj, inf, i, ration, partySize, ref mileage);
            }
            else
                CheckIllness(r, hp, dead, inj, inf, i, ration, partySize, ref mileage);

            if (dead[0]) return Outcome.Death;

            if (food > 0)
            {
                int liveNow = 0; for (int j = 0; j < partySize; j++) if (!dead[j]) liveNow++;
                food = Math.Max(0, food - (4 - ration) * liveNow);
                Heal(r, hp, dead, i);
            }
            else
            {
                Damage(r, hp, dead, inj, inf, i, 10, 50);
                if (dead[0]) return Outcome.Starved;
            }
        }
        return dead[0] ? Outcome.Death : (Outcome?)null;
    }

    // ---- DeliveryOffer.RealizedTip (faithful): tip-bait 15% / unicorn 10% / else ~55-70% of the estimate. ----
    static double RealizedTip(Rng r, double estTip)
    {
        double roll = r.NextDouble();
        if (roll < 0.15) return r.NextDouble() * 1.0;       // tip-bait: collapses to loose change
        if (roll < 0.25) return 8.0 + r.NextDouble() * 7.0; // unicorn: a genuinely generous tip
        return estTip * (0.55 + r.NextDouble() * 0.15);     // common case: lands below the estimate
    }

    // ---- Person.CheckIllness (faithful) ----
    static void CheckIllness(Rng r, int[] hp, bool[] dead, bool[] inj, bool[] inf, int i, int ration, int partySize, ref double mileage)
    {
        if (dead[i]) return;
        if (r.Bool()) return; // 50% no-op

        if (r.Next(100) <= 10 + 35 * (ration - 1))
        {
            ReduceMileage(ref mileage, 5);
            Damage(r, hp, dead, inj, inf, i, 10, 50);
        }
        else if (r.Next(100) <= 5 - 40 / partySize * (ration - 1))
        {
            ReduceMileage(ref mileage, 15);
            Damage(r, hp, dead, inj, inf, i, 10, 50);
        }

        // infection/injury complications (only bite if flagged)
        if (dead[i]) return;
        var band = Band(hp[i]);
        if ((inf[i] || inj[i]))
        {
            if (band == 500) { ReduceMileage(ref mileage, 5); Damage(r, hp, dead, inj, inf, i, 10, 50); }
            else if (band == 400 && r.Bool()) { ReduceMileage(ref mileage, 5); Damage(r, hp, dead, inj, inf, i, 10, 50); }
            else if (band == 300) { ReduceMileage(ref mileage, 10); Damage(r, hp, dead, inj, inf, i, 5, 10); }
            else if (band == 200) { ReduceMileage(ref mileage, 15); Damage(r, hp, dead, inj, inf, i, 1, 5); }
        }
    }

    static void ReduceMileage(ref double mileage, int amt)
    {
        if (mileage <= 0) return;
        mileage = Math.Max(0, mileage - amt);
    }

    // ---- Person.Damage (faithful incl. 2% Person-category event) ----
    static void Damage(Rng r, int[] hp, bool[] dead, bool[] inj, bool[] inf, int i, int min, int max)
    {
        if (dead[i]) return;
        hp[i] -= r.Next(min, max);
        if (hp[i] <= 0) { hp[i] = 0; dead[i] = true; return; }
        // !Infected || !Injured is true unless BOTH set -> Person event 2%
        if (!(inf[i] && inj[i]) && r.Next(100) < 2)
        {
            int pick = r.Next(100);
            if (pick < 45) inj[i] = true;        // injury (BrokenArm-style)
            else if (pick < 80) inf[i] = true;   // infection
            // else minor / no flag
        }
    }

    // ---- Person.Heal (faithful) ----
    static void Heal(Rng r, int[] hp, bool[] dead, int i)
    {
        if (dead[i] || hp[i] >= 500) return;
        if (r.Bool()) return;
        hp[i] = Math.Min(500, hp[i] + r.Next(1, 10));
    }

    enum VehOut { None, Stranded }
    // ---- Vehicle event 4%/day: uniform over the 11 authored Vehicle events ----
    static VehOut VehicleEvent(Rng r, ref int gas, ref int tires, ref int alts, ref int trans,
        ref int cloth, ref int ammo, ref int food)
    {
        int pick = r.Next(11);
        switch (pick)
        {
            case 0: // BrokenVehiclePart -> break random installed part; need matching spare or strand
                int part = r.Next(3); // 0 wheel,1 axle,2 tongue
                if (part == 0) { if (tires > 0) { tires--; return VehOut.None; } return VehOut.Stranded; }
                if (part == 1) { if (alts > 0) { alts--; return VehOut.None; } return VehOut.Stranded; }
                if (trans > 0) { trans--; return VehOut.None; } return VehOut.Stranded;
            case 1: // OxenDied -> -1 gas can
                if (gas > 0) gas--;
                return gas <= 0 ? VehOut.Stranded : VehOut.None;
            case 2: // TippedVehicle (ItemDestroyer)
            case 3: // VehicleFire (ItemDestroyer)
                DestroyRandomItems(r, ref gas, ref tires, ref alts, ref trans, ref cloth, ref ammo, ref food);
                return VehOut.None;
            default: // 4..10: lose-time / wander / repair-flavor -> negligible to survival economy
                return VehOut.None;
        }
    }

    // ---- Vehicle.DestroyRandomItems (faithful: per stack 50% skip else destroy rand(1,qty)) ----
    static void DestroyRandomItems(Rng r, ref int gas, ref int tires, ref int alts, ref int trans,
        ref int cloth, ref int ammo, ref int food)
    {
        void Z(ref int q) { if (q >= 1 && !r.Bool()) q -= r.Next(1, q + 1); }
        Z(ref gas); Z(ref cloth); Z(ref ammo); Z(ref tires); Z(ref alts); Z(ref trans); Z(ref food);
        if (gas < 0) gas = 0; if (food < 0) food = 0;
    }

    // ---- end-game scoring (FinalPoints faithful) ----
    static Result Win(string prof, int[] hp, bool[] dead,
        int gas, int tires, int alts, int trans, int cloth, int ammo, int food, int cash, int day, int partySize)
    {
        var livingHealth = new List<int>();
        for (int i = 0; i < partySize; i++) if (!dead[i]) livingHealth.Add(Band(hp[i]));
        int living = livingHealth.Count;
        if (living == 0) return Lose(Outcome.Death, day);
        int avg = (int)livingHealth.Average();
        int avgBand = ClosestBand(avg);

        int pts = 0;
        pts += living * avgBand;          // people health
        // SUV reference item has quantity 0 -> 0 pts (matches FinalPoints)
        pts += gas * 4;                   // gas cans
        pts += (tires + alts + trans) * 2;// spare parts
        pts += cloth * 2;                 // leggings
        pts += ammo / 50;                 // ammo
        pts += food / 25;                 // snacks
        pts += cash / 5;                  // cash
        int total = pts * ProfMult(prof);
        string rating = total < 3000 ? "Greenhorn" : total < 6000 ? "Adventurer" : "TrailGuide";
        return new Result { Outcome = Outcome.Win, Days = day, Score = total, EndCash = cash, Living = living, Rating = rating };
    }

    static Result Lose(Outcome o, int day) => new Result { Outcome = o, Days = day, Score = 0, EndCash = 0, Living = 0, Rating = "Dead" };

    static int Band(int h) => h > 400 ? 500 : h > 300 ? 400 : h > 200 ? 300 : h > 0 ? 200 : 0;
    static int ClosestBand(int v)
    {
        int[] bands = { 500, 400, 300, 200 };
        int best = bands[0], bd = int.MaxValue;
        foreach (var b in bands) { int d = Math.Abs(b - v); if (d < bd) { bd = d; best = b; } }
        return best;
    }

    // ---- WolfCurses-style RNG (System.Random semantics) ----
    sealed class Rng
    {
        readonly Random _r;
        public Rng(int seed) { _r = new Random(seed); }
        public int Next(int maxEx) => _r.Next(maxEx);
        public int Next(int min, int maxEx) => _r.Next(min, maxEx);
        public bool Bool() => _r.Next(0, 2) == 1;
        public double NextDouble() => _r.NextDouble();
    }

    // ---- helpers ----
    static Dictionary<string, string> ParseArgs(string[] a)
    {
        var d = new Dictionary<string, string>();
        for (int i = 0; i < a.Length; i++)
            if (a[i].StartsWith("--") && i + 1 < a.Length) { d[a[i].Substring(2)] = a[i + 1]; i++; }
        return d;
    }
    static int I(Dictionary<string, string> d, string k, int def) =>
        d.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : def;
    static int Median(List<int> xs)
    {
        if (xs.Count == 0) return 0;
        var s = xs.OrderBy(x => x).ToList();
        return s[s.Count / 2];
    }
    static double MedianD(List<double> xs)
    {
        if (xs.Count == 0) return 0;
        var s = xs.OrderBy(x => x).ToList();
        return Math.Round(s[s.Count / 2], 2);
    }
    static string Json(IEnumerable<(string, object)> fields)
    {
        var sb = new StringBuilder("{");
        bool first = true;
        foreach (var (k, v) in fields)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(k).Append("\":");
            if (v is string s) sb.Append('"').Append(s.Replace("\"", "'")).Append('"');
            else if (v is bool b) sb.Append(b ? "true" : "false");
            else sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture));
        }
        sb.Append('}');
        return sb.ToString();
    }
}
