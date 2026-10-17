using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using OregonTrailDotNet.Module.Creator;
using OregonTrailDotNet.Web;
using OregonTrailDotNet.Window.Travel.Creator;

internal static class CreatorTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception($"Creator: {message}");
    }
    private static VideoIdea Idea(int score, string id = "test", params string[] locations) =>
        new(id, $"A topic {id}", "Test footage.", id, score, locations, 1);
    private sealed class Wallet(int initial)
    {
        public int Balance = initial;
        public int Credits;
        public CreatorChannel Open(Random random, CreatorCareer career = null, params VideoIdea[] ideas) =>
            new(random, () => Balance, delta =>
            {
                Balance += delta;
                if (delta > 0) Credits++;
                Check(Balance >= 0, "cash may not overdraw");
            }, career ?? new CreatorCareer(), ideas.Length == 0 ? null : ideas);
    }
    private sealed class FixedRandom(double value) : Random
    {
        public override double NextDouble() => value;
        public override int Next(int maxValue) => 0;
    }
    private sealed class SequenceRandom(params double[] values) : Random
    {
        private int _index;
        public override double NextDouble() => values[_index++ % values.Length];
        public override int Next(int maxValue) => 0;
    }

    public static void RunRules()
    {
        var broke = new Wallet(7);
        var channel = broke.Open(new Random(1), null, Idea(25));
        Check(!channel.Start("<script>") && !channel.Start("---") && !channel.Start(new string('a', 33)), "names validated");
        Check(channel.Start("Family & Fog") && !channel.Start("Again"), "channel starts once");
        Check(!channel.Film(null, false, "Road", 0) && !channel.Publish(0) && broke.Balance == 7, "unaffordable filming has no side effects");
        Check(!channel.Buy("invented", 0) && !channel.Buy("light-clip", 0) && !channel.Withdraw(), "invalid gear, budget, and payout rejected");

        var wallet = new Wallet(5000);
        var career = new CreatorCareer();
        channel = wallet.Open(new Random(42), career, Idea(20));
        channel.Start("Debt Test");
        var originalCeiling = channel.Ceiling;
        Check(!channel.Buy("lens-prime", 0) && !channel.Buy("monitor-basic", 0), "phone cannot buy interchangeable lenses or HDMI monitor");
        Check(channel.Buy("mirrorless", 0) && !channel.Buy("mirrorless", 0), "order charges once");
        Check(wallet.Balance == 4251 && career.GearSpent == 749 && channel.Ceiling == originalCeiling, "in-transit gear has no effect");
        channel.ReceiveOrders(0);
        Check(!channel.Equip("mirrorless"), "cannot equip undelivered gear");
        channel.ReceiveOrders(1);
        Check(channel.Equip("mirrorless") && !channel.Equip("light-clip") && !channel.Equip("mirrorless"), "only owned cameras can equip");
        Check(channel.Buy("lens-prime", 1) && channel.Buy("monitor-basic", 1) && !channel.Buy("lens-cinema", 1), "camera mount and HDMI enforced");
        channel.ReceiveOrders(2);
        Check(channel.Ceiling == 240000 + 15000 + 12000, "compatible gear only raises cap");
        Check(channel.Buy("light-clip", 2) && channel.Buy("light-panel", 2), "support items ordered");
        channel.ReceiveOrders(3);
        Check(channel.Ceiling == 240000 + 15000 + 12000 + 14000, "only strongest supporting item counts");
        Check(channel.Equip("phone") && channel.Ceiling == 50000 + 14000, "switching to phone detaches incompatible gear");
        Check(channel.Film(null, false, "Road", 3), "film drafts");
        var capturedCap = career.Draft.Ceiling;
        Check(!channel.Film(null, false, "Road", 3) && career.DaysWorked == 1, "one draft only and no duplicate cost");
        channel.Equip("mirrorless");
        Check(career.Draft.Ceiling == capturedCap, "new camera cannot change existing footage");
        var reopened = wallet.Open(new Random(999), career, Idea(20));
        Check(reopened.Career.Draft == career.Draft && reopened.Camera.Id == "mirrorless", "career persists across studio visits");
        Check(reopened.Publish(4) && !reopened.Publish(4), "publication consumes draft exactly once");
        Check(career.Uploads == 1 && career.Videos.Single().Views <= 360 && career.ProductionSpent == 8, "dull footage stays dull with costly gear");
        Check(wallet.Balance == 5000 - career.GearSpent - career.ProductionSpent + career.PaidOut, "wallet reconciles with all costs");
        Check(!channel.Discard() && !channel.Reedit(100, 4), "missing footage cannot mutate career");
        Check(channel.Film(null, false, "Road", 5), "new footage can be captured after publishing");
        var afterFilm = wallet.Balance;
        Check(channel.Discard() && !channel.Discard() && wallet.Balance == afterFilm && career.DaysWorked == 2,
            "discard cannot refund production or trail-day costs");
        var fresh = new CreatorCareer();
        Check(!fresh.Started && fresh.Subscribers == 0 && fresh.Owned.SetEquals(new[] { "phone" }), "new journeys have independent careers");

        // Identical demand below the old ceiling must produce identical views, subscribers, earnings, and RNG continuation.
        foreach (var score in new[] { 20, 55, 90 })
        {
            var a = new Wallet(10000).Open(new FixedRandom(.5), null, Idea(score));
            var b = new Wallet(10000).Open(new FixedRandom(.5), null, Idea(score));
            a.Start("Phone"); b.Start("Cinema");
            b.Buy("cinema", 0); b.ReceiveOrders(1); b.Equip("cinema");
            for (var i = 0; i < 4; i++)
            {
                a.Film(null, false, "Road", i); b.Film(null, false, "Road", i);
                a.Publish(i); b.Publish(i);
                Check(a.Career.Videos[0].Views == b.Career.Videos[0].Views && a.Career.Subscribers == b.Career.Subscribers &&
                    a.Career.AlgorithmScore == b.Career.AlgorithmScore, "gear never boosts sub-ceiling demand");
            }
        }

        // Force a breakout + moderation hit, retaining genuine production and payout paths. The second value is the
        // good-idea viral roll, kept off so this stays an ordinary, ceiling-capped breakout.
        var jackpot = new Wallet(1000);
        channel = jackpot.Open(new SequenceRandom(0, .99, .99, .5, .8, .9, .1, .7, .1, .5), null, Idea(95));
        channel.Start("Probably Fine");
        channel.Film(null, false, "Road", 0); channel.Publish(1);
        var flagged = channel.Career.Videos.Single();
        Check(flagged.Views == 50000 && flagged.Demonetized && flagged.Revenue == 0 && channel.Career.Monetized, "15% roll blocks eligible ads");
        var oldSubs = channel.Career.Subscribers;
        Check(channel.Reedit(flagged.Id, 2), "flagged upload can be repaired");
        var repaired = channel.Career.Videos.Single();
        Check(repaired.Views == flagged.Views / 2 && repaired.Reedited && !repaired.Demonetized &&
            channel.Career.Subscribers == oldSubs && repaired.Revenue == 125, "reupload halves views and earns once without duplicate subscribers");
        Check(!channel.Reedit(flagged.Id, 3) && channel.Career.DaysWorked == 2 && channel.Career.Reuploads == 1, "re-edit day and receipt applied once");
        Check(channel.Withdraw() && !channel.Withdraw() && jackpot.Credits == 1 && jackpot.Balance == 1117, "withdrawal credits road cash exactly once");

        var weak = new Wallet(10000).Open(new FixedRandom(.5), null, Idea(10));
        var great = new Wallet(10000).Open(new FixedRandom(.5), null, Idea(95));
        weak.Start("Weak"); great.Start("Great");
        weak.Film(null, false, "Road", 0); great.Film(null, false, "Road", 0);
        weak.Publish(1); great.Publish(1);
        Check(weak.Career.AlgorithmScore < 1 && great.Career.AlgorithmScore > 1, "hidden algorithm moves both ways");
        weak.Career.Subscribers = 1000000;
        weak.Film(null, false, "Road", 1); weak.Publish(2);
        Check(weak.Career.Videos[0].Views <= 360, "even a million subscribers cannot turn a weak idea into a viral video");
        great.Film(null, false, "Road", 1); great.Publish(2);
        Check(great.Career.Videos[0].SubscriberViews > 0 && great.Career.Videos[0].DiscoveryViews > 0, "uploads have both returning and discovery viewers");

        var local = Idea(90, "local", "bucees");
        Check(CreatorCatalog.Eligible(local, "bucees", true) && !CreatorCatalog.Eligible(local, "bucees", false) &&
            !CreatorCatalog.Eligible(local, "wall-drug", true), "geo footage requires actual arrival at exact location");
        var locked = new Wallet(50).Open(new Random(1), null, local);
        locked.Start("Locked");
        Check(!locked.Film("bucees", false, "Road", 0) && locked.Career.ProductionSpent == 0 &&
            locked.Film("bucees", true, "Buc-ee's", 0), "filming command enforces geo locks without wasting money");
        Check(locked.Publish(1), "captured footage can be published later");

        var flaggedCount = 0;
        const int samples = 20000;
        for (var seed = 0; seed < samples; seed++)
        {
            var c = new Wallet(20).Open(new Random(seed), null, Idea(90));
            c.Start("Test"); c.Film(null, false, "Road", 0); c.Publish(1);
            if (c.Career.Videos[0].Demonetized) flaggedCount++;
        }
        Check(flaggedCount > samples * .14 && flaggedCount < samples * .16, "moderation frequency must remain approximately15%");
        var goodCount = 0;
        var goodDraws = 0;
        for (var seed = 0; seed < 2000; seed++)
        {
            var c = new Wallet(1000).Open(new Random(seed));
            c.Start("Pitch Meeting");
            for (var day = 0; day < 24; day++)
            {
                c.Film(null, false, "Road", day);
                goodDraws++;
                if (c.Career.Draft.Idea.Potential >= CreatorChannel.GoodIdeaPotential) goodCount++;
                c.Discard();
            }
        }
        Check(goodCount > goodDraws * .31 && goodCount < goodDraws * .33, "good ideas must remain approximately 32% of filming draws");
        Console.WriteLine($"PASS creator rules: wallet, delivery, compatibility, gear ceiling only, drafts, geo locks, hidden algorithm, audiences, exact-once re-edit/payout; moderation {flaggedCount}/{samples} ({100.0 * flaggedCount / samples:F2}%); good ideas {goodCount}/{goodDraws} ({100.0 * goodCount / goodDraws:F2}%)");
    }

    // Publish draws for a single good idea: breakout, viral, interest, regulars, conversion gate, conversion, churn,
    // discovery noise, limited ads, RPM. Bad ideas skip the viral draw.
    private static SequenceRandom Viral(double conversionGate = .5, double limitedAds = .99) =>
        new(.5, 0, .5, .5, conversionGate, .5, .5, .5, limitedAds, .5);

    private static CreatorChannel PublishViral(Wallet wallet, SequenceRandom random)
    {
        var channel = wallet.Open(random, null, Idea(90));
        channel.Start("Lightning");
        channel.Film(null, false, "Road", 0);
        channel.Publish(1);
        Check(channel.Career.LicensePending && channel.Career.Videos[0].Viral, "forced roll goes viral");
        return channel;
    }

    private static (string[] Ids, string[] Bound) Actions(CreatorChannel channel)
    {
        var desk = (CreatorDesk)RuntimeHelpers.GetUninitializedObject(typeof(CreatorDesk));
        typeof(CreatorDesk).GetProperty("Channel")!.SetValue(desk, channel);
        typeof(CreatorDesk).GetProperty("Tab")!.SetValue(desk, "studio");
        var bindings = new Dictionary<string, PresentationActionBinding>();
        var build = typeof(PresentationCatalog).GetMethod("BuildCreatorActions", BindingFlags.NonPublic | BindingFlags.Static)!;
        var actions = (List<GameActionDto>)build.Invoke(null, new object[] { desk, bindings })!;
        if (channel.Career.LicensePending)
            Check(!desk.Execute("film", null) && !desk.Execute("leave", null) && !desk.Execute("tab.shop", null),
                "desk rejects every other command while an offer is pending");
        return (actions.Select(a => a.ActionId).ToArray(), bindings.Keys.ToArray());
    }

    private static CreatorLicenseDto License(CreatorCareer career) => (CreatorLicenseDto)typeof(PresentationCatalog)
        .GetMethod("BuildLicense", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new object[] { career });

    public static void RunViral()
    {
        // Only good ideas can go viral, and the boundary is the same GoodIdeaPotential used by #34.
        var almost = new Wallet(1000).Open(new FixedRandom(0), null, Idea(CreatorChannel.GoodIdeaPotential - 1));
        almost.Start("Almost");
        almost.Film(null, false, "Road", 0); almost.Publish(1);
        Check(!almost.Career.LicensePending && !almost.Career.Videos[0].Viral && almost.Career.Videos[0].Views <= 2400,
            "a bad idea cannot go viral even on a winning roll");
        var hit = new Wallet(1000).Open(new FixedRandom(0), null, Idea(CreatorChannel.GoodIdeaPotential));
        hit.Start("Barely Good");
        hit.Film(null, false, "Road", 0); hit.Publish(1);
        Check(hit.Career.LicensePending && hit.Career.Videos[0].Viral && hit.Career.Videos[0].Views >= 200000,
            "a good idea can go viral past the phone's 50,000 ceiling");

        // Equipment never boosts or blocks a viral video: phone and cinema kit get identical outcomes.
        var phone = PublishViral(new Wallet(10000), Viral());
        var cinema = new Wallet(10000).Open(Viral(), null, Idea(90));
        cinema.Start("Rented Prestige");
        cinema.Buy("cinema", 0); cinema.ReceiveOrders(1); cinema.Equip("cinema");
        cinema.Film(null, false, "Road", 1); cinema.Publish(2);
        Check(phone.Career.Videos[0].Views == 400000 && cinema.Career.Videos[0].Views == 400000 &&
            phone.Career.Subscribers == cinema.Career.Subscribers, "viral views ignore the kit ceiling and gear");

        // While pending: the RPM and ad money are hidden, nothing else can happen, and no trail day is spent.
        var held = phone.Career.Videos[0];
        Check(held.Revenue == 0 && held.Rpm == 0 && phone.Career.Revenue == 0, "held ad money is not shown or credited early");
        Check(phone.Career.License.Offer == 1600 && CreatorChannel.LicenseOffer(400000) == 1600, "offer is $4 per 1,000 views");
        Check(!phone.CanFilm && !phone.CanWithdraw && phone.BuyBlock(CreatorCatalog.Gear[0]) != null &&
            !phone.Buy(CreatorCatalog.Gear[0].Id, 1) && !phone.Reedit(held.Id, 1) && !phone.WaitForOrders() &&
            phone.Career.DaysWorked == 1, "the offer blocks every other studio action and costs no day");

        // Accept: pays road cash once, forfeits the held ads and the re-edit.
        var accepting = new Wallet(1000);
        var buyer = PublishViral(accepting, Viral());
        var cashBefore = accepting.Balance;
        Check(buyer.AcceptLicense() && !buyer.AcceptLicense() && !buyer.DeclineLicense(), "an offer settles exactly once");
        var sold = buyer.Career.Videos[0];
        Check(accepting.Balance == cashBefore + 1600 && accepting.Credits == 1 && buyer.Career.LicenseIncome == 1600 &&
            buyer.Career.Revenue == 0 && sold.Licensed && sold.LicensePaid == 1600 && sold.Revenue == 0 && sold.Rpm == 5m,
            "accepting pays the lump sum and forfeits ad revenue");
        Check(buyer.Career.Net == 1600 - CreatorChannel.ProductionCost && buyer.CanFilm, "license income counts toward net; studio reopens");

        // Decline on a monetized, clean video: exactly the normal ad receipt, released once.
        var keeper = PublishViral(new Wallet(1000), Viral());
        Check(keeper.Career.Monetized && keeper.DeclineLicense() && !keeper.DeclineLicense() && !keeper.AcceptLicense(),
            "declining settles exactly once");
        Check(keeper.Career.Revenue == 2000 && keeper.Career.Videos[0].Revenue == 2000 && keeper.Career.LicenseIncome == 0 &&
            !keeper.Career.Videos[0].Licensed, "declining pays the normal ad rate: 400,000 views at $5.00");

        // Limited ads: declining keeps the normal re-edit (half views, no new subscribers); accepting forfeits it.
        var flagged = PublishViral(new Wallet(1000), Viral(limitedAds: .1));
        Check(flagged.Career.Monetized && flagged.Career.Videos[0].Demonetized && flagged.Career.Videos[0].Incident != null,
            "the limited-ads result is visible before the decision");
        var subscribers = flagged.Career.Subscribers;
        flagged.DeclineLicense();
        Check(flagged.Career.Revenue == 0 && flagged.Reedit(1, 2), "declined limited-ads video earns nothing until re-edited");
        var repaired = flagged.Career.Videos[0];
        Check(repaired.Views == 200000 && repaired.Revenue == 1000 && flagged.Career.Subscribers == subscribers,
            "re-edit of a declined viral video follows the normal half-view rule");
        var flaggedSale = PublishViral(new Wallet(1000), Viral(limitedAds: .1));
        flaggedSale.AcceptLicense();
        Check(!flaggedSale.Reedit(1, 2) && flaggedSale.Career.Reuploads == 0, "a sold video cannot be re-edited");

        // Unmonetized: declining pays nothing, so the offer is strictly better.
        var unpaid = PublishViral(new Wallet(1000), Viral(conversionGate: .1));
        Check(!unpaid.Career.Monetized && unpaid.Career.Subscribers == 0, "fixture: a viral video that converts nobody");
        unpaid.DeclineLicense();
        Check(unpaid.Career.Revenue == 0 && unpaid.Career.Net < 0, "declining as an unpartnered channel earns nothing");

        // Only the two answers are advertised (and bound) while an offer is pending; neither exists otherwise.
        var pending = PublishViral(new Wallet(1000), Viral());
        var (ids, bound) = Actions(pending);
        var answers = new[] { "creator.license-accept", "creator.license-decline" };
        Check(ids.OrderBy(i => i).SequenceEqual(answers) && bound.OrderBy(i => i).SequenceEqual(answers),
            "pending offer advertises only its two answers");
        var dto = License(pending.Career);
        Check(dto.Offer == 1600 && dto.Views == 400000 && dto.AdLow == 1200 && dto.AdHigh == 2800 && dto.Monetized &&
            typeof(CreatorLicenseDto).GetProperties().All(p => p.Name is not ("Rpm" or "Revenue" or "Potential")),
            "offer projects only public bounds, never the held RPM or revenue");
        pending.DeclineLicense();
        (ids, _) = Actions(pending);
        Check(!ids.Any(i => i.StartsWith("creator.license")) && ids.Contains("creator.film") && License(pending.Career) == null,
            "license actions vanish once answered");

        // Rate: about 0.5% of good originals, and never a bad one.
        var good = 0;
        var goodViral = 0;
        for (var seed = 0; seed < 2000; seed++)
        {
            var c = new Wallet(1000).Open(new Random(seed), null, Idea(90));
            c.Start("Rate");
            for (var day = 0; day < 100; day++)
            {
                c.Film(null, false, "Road", day); c.Publish(day);
                good++;
                if (c.Career.LicensePending) { goodViral++; c.DeclineLicense(); }
            }
        }
        var bad = 0;
        for (var seed = 0; seed < 500; seed++)
        {
            var c = new Wallet(1000).Open(new Random(seed), null, Idea(60));
            c.Start("Rate");
            for (var day = 0; day < 100; day++)
            {
                c.Film(null, false, "Road", day); c.Publish(day);
                Check(!c.Career.LicensePending, "bad ideas never go viral");
                bad++;
            }
        }
        Check(goodViral > good * .0045 && goodViral < good * .0055, $"viral rate ~0.5% of good originals, observed {100.0 * goodViral / good:F3}%");
        Console.WriteLine($"PASS creator viral: good-only, ceiling bypass, gear-neutral, hidden held ads, modal offer, exact-once accept/decline, re-edit forfeiture, advertised only while pending; {goodViral}/{good} good originals viral ({100.0 * goodViral / good:F3}%), 0/{bad} bad");
    }

    public static void RunCatalog()
    {
        var ideas = CreatorCatalog.Ideas;
        Check(ideas.Count == 200, "exactly 200 authored ideas required");
        Check(ideas.Select(i => i.Id).Distinct().Count() == 200 && ideas.Select(i => i.ConceptKey).Distinct().Count() == 200,
            "idea IDs and concepts must be unique");
        Check(ideas.Select(i => Regex.Replace(i.Title.ToLowerInvariant(), @"[^\p{L}\p{N}]", "")).Distinct().Count() == 200,
            "normalized titles must be unique");
        Check(ideas.All(i => i.Potential is >= 1 and <= 100 && !string.IsNullOrWhiteSpace(i.Premise)), "ideas have scored potential and premises");
        Check(ideas.GroupBy(i => i.AuthorBatch).Count() == 20 && ideas.GroupBy(i => i.AuthorBatch).All(g => g.Count() == 10),
            "twenty authors each supply ten ideas");
        Check(ideas.Count(i => i.Potential < 36) >= 120 && ideas.Count(i => i.Potential >= 80) == 20, "mostly bad ideas and scarce great ideas");
        Check(ideas.Count(i => i.Locations.Length == 0) >= 160 && ideas.All(i => i.Locations.All(CreatorCatalog.Locations.Values.Contains)),
            "large generic pool and only actual geography");
        foreach (var location in CreatorCatalog.Locations.Values)
            Check(ideas.Where(i => CreatorCatalog.Eligible(i, location, false)).All(i => i.Locations.Length == 0), "no location footage mid-route");
        Console.WriteLine("PASS creator catalog: 200 unique scored ideas, 20 author batches, route locks, mostly weak concepts");
    }

    /// <param name="license">"rational" sells only when the views cannot pay; "accept" always sells; "decline" never does.</param>
    public static void RunBalance(bool enforceTarget = true, string license = "rational")
    {
        const int samples = 10000;
        foreach (var policy in new[] { "phone-12", "light-12", "mirrorless-12", "phone-24" })
        {
            var wins = 0;
            decimal net = 0;
            var monetize = 0;
            var viral = 0;
            var sold = 0;
            var viralCareers = 0;
            // Offer vs held ad money, by what the player could see: partnered + clean, partnered + limited ads, unpartnered.
            var cases = new Dictionary<string, (int Count, decimal Offer, decimal Ads)>
                { ["clean"] = default, ["limited"] = default, ["unpartnered"] = default };
            for (var seed = 0; seed < samples; seed++)
            {
                var wallet = new Wallet(5000);
                var c = wallet.Open(new Random(seed));
                c.Start("Road Sample");
                if (policy == "light-12") c.Buy("light-clip", 0);
                if (policy == "mirrorless-12") c.Buy("mirrorless", 0);
                c.ReceiveOrders(1);
                if (policy == "mirrorless-12") c.Equip("mirrorless");
                var days = 1;
                var uploads = policy == "phone-24" ? 24 : 12;
                for (var film = 0; film < uploads; film++)
                {
                    Check(c.Film(null, false, "Road", days++), "cohort can afford its production policy");
                    c.Publish(days);
                    if (c.Career.LicensePending)
                    {
                        viral++;
                        var kind = !c.Career.Monetized ? "unpartnered" : c.Career.Videos[0].Demonetized ? "limited" : "clean";
                        var held = cases[kind];
                        cases[kind] = (held.Count + 1, held.Offer + c.Career.License.Offer, held.Ads + c.Career.License.Revenue);
                        // The declared player sells when the views cannot pay (no partner status or limited ads)
                        // and gambles on ads otherwise.
                        if (license == "accept" || license == "rational" && kind != "clean") { c.AcceptLicense(); sold++; }
                        else c.DeclineLicense();
                    }
                    if (c.Career.Videos[0].Demonetized) c.Reedit(c.Career.Videos[0].Id, days++);
                    if (c.CanWithdraw) c.Withdraw();
                    Check(c.Career.AlgorithmScore is >= .35 and <= 2.2, "hidden multiplier bounded");
                }
                Check(wallet.Balance == 5000 - c.Career.GearSpent - c.Career.ProductionSpent + c.Career.PaidOut +
                    c.Career.LicenseIncome, "entire cohort reconciles actual road cash");
                if (c.Career.ViralHits > 0) viralCareers++;
                if (c.Career.Net > 0) wins++;
                if (c.Career.Monetized) monetize++;
                net += c.Career.Net;
            }
            if (enforceTarget && policy == "phone-12")
                Check(wins >= samples * .06 && wins <= samples * .10, $"frugal12-upload net-profit target ~8%, observed {100.0 * wins / samples:F2}%");
            Check(wins > 0 && wins < samples / 4, "all declared policies retain rare upside and mostly losses");
            Console.WriteLine($"PASS creator balance / {policy} / Viral Hog {license}: {wins}/{samples} net-positive ({100.0 * wins / samples:F2}%), {monetize} monetized, mean net ${net / samples:F2}; viral {viral} uploads in {viralCareers} careers, {sold} sold");
            Console.WriteLine("    Viral Hog offers vs held ads: " + string.Join("; ", cases.Select(pair => pair.Value.Count == 0 ? $"{pair.Key} 0" :
                $"{pair.Key} {pair.Value.Count}, mean offer ${pair.Value.Offer / pair.Value.Count:F0}, mean ads ${pair.Value.Ads / pair.Value.Count:F0}")));
        }
    }
}
