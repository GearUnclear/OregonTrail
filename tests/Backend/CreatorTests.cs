using System.Text.RegularExpressions;
using OregonTrailDotNet.Module.Creator;

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

        // Force a breakout + moderation hit, retaining genuine production and payout paths.
        var jackpot = new Wallet(1000);
        channel = jackpot.Open(new SequenceRandom(0, .99, .5, .8, .9, .1, .7, .1, .5), null, Idea(95));
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
        Console.WriteLine($"PASS creator rules: wallet, delivery, compatibility, gear ceiling only, drafts, geo locks, hidden algorithm, audiences, exact-once re-edit/payout; moderation {flaggedCount}/{samples} ({100.0 * flaggedCount / samples:F2}%)");
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

    public static void RunBalance(bool enforceTarget = true)
    {
        const int samples = 10000;
        foreach (var policy in new[] { "phone-12", "light-12", "mirrorless-12", "phone-24" })
        {
            var wins = 0;
            decimal net = 0;
            var monetize = 0;
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
                    if (c.Career.Videos[0].Demonetized) c.Reedit(c.Career.Videos[0].Id, days++);
                    if (c.CanWithdraw) c.Withdraw();
                    Check(c.Career.AlgorithmScore is >= .35 and <= 2.2, "hidden multiplier bounded");
                }
                Check(wallet.Balance == 5000 - c.Career.GearSpent - c.Career.ProductionSpent + c.Career.PaidOut,
                    "entire cohort reconciles actual road cash");
                if (c.Career.Net > 0) wins++;
                if (c.Career.Monetized) monetize++;
                net += c.Career.Net;
            }
            if (enforceTarget && policy == "phone-12")
                Check(wins >= samples * .06 && wins <= samples * .10, $"frugal12-upload net-profit target ~8%, observed {100.0 * wins / samples:F2}%");
            Check(wins > 0 && wins < samples / 4, "all declared policies retain rare upside and mostly losses");
            Console.WriteLine($"PASS creator balance / {policy}: {wins}/{samples} net-positive ({100.0 * wins / samples:F2}%), {monetize} monetized, mean net ${net / samples:F2}");
        }
    }
}
