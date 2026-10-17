using System.Reflection;
using System.Text.Json;
using OregonTrailDotNet.Module.Creator;
using OregonTrailDotNet.Web;
using OregonTrailDotNet.Window.Travel.Creator;

/// <summary>
/// Viral Hog through a real journey: the actual GameEngine dispatch, CreatorDesk, road-cash wallet, and snapshot.
/// Only the channel's injected RNG is replaced, so a 0.5% viral roll can be forced deterministically.
/// </summary>
internal static class CreatorJourneyTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class SequenceRandom(params double[] values) : Random
    {
        private int _index;
        public override double NextDouble() => values[_index++ % values.Length];
        public override int Next(int maxValue) => 0;
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    public static async Task Run()
    {
        await Journey(accept: true);
        await Journey(accept: false);
        Console.WriteLine("PASS creator viral journey: real desk dispatch, modal 400s, stale 409, no trail day, hidden held ads in JSON, road cash once, sold video loses re-edit, declined video pays the channel");
    }

    private static Task<GameActionResponseDto> Act(GameEngine engine, string id, string text = null, int? value = null) =>
        engine.DispatchAsync(new GameActionRequest(id, engine.Snapshot.Revision, text, value), default);

    private static async Task Accepted(GameEngine engine, string id, string text = null, int? value = null)
    {
        var result = await Act(engine, id, text, value);
        Check(result.Accepted, $"{id} accepted: {result.ErrorCode} {result.Message}");
    }

    private static async Task Rejected(GameEngine engine, string id, string code)
    {
        var revision = engine.Snapshot.Revision;
        var result = await Act(engine, id);
        Check(!result.Accepted && result.ErrorCode == code && engine.Snapshot.Revision == revision,
            $"{id} rejected as {code}, got {result.ErrorCode}");
    }

    private static async Task Settle(GameEngine engine)
    {
        for (var i = 0; i < 4; i++) { engine.Tick(); await Task.Delay(40); }
    }

    private static CreatorCareer Career(GameEngine engine)
    {
        var game = typeof(GameEngine).GetField("_game", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(engine)!;
        var desk = (CreatorDesk)game.GetType().GetProperty("CurrentForm")!.GetValue(game)!;
        return desk.Channel.Career;
    }

    // Mirrors prepare_trip in tests/backend_integration.py: minivan, default packing, a small supply run.
    private static async Task PrepareTrip(GameEngine engine)
    {
        var typed = 0;
        for (var step = 0; step < 60; step++)
        {
            await Settle(engine);
            var state = engine.Snapshot;
            var screen = state.Screen;
            if (screen.Kind == "travel") return;
            if (screen.Id.EndsWith("packthecardecision")) await Accepted(engine, screen.Actions[1].ActionId);
            else if (screen.Kind == "store")
            {
                foreach (var (item, amount) in new[] { ("gas", 10), ("snacks", 150), ("tire", 2) })
                {
                    var row = engine.Snapshot.Store.Rows.FirstOrDefault(r => r.ItemId == item);
                    if (row != null) await Accepted(engine, row.SetActionId, value: Math.Min(amount, row.MaxQuantity));
                }
                await Accepted(engine, "store.checkout");
            }
            else if (screen.Input != null)
                await Accepted(engine, screen.Actions.First(a => a.Enabled).ActionId,
                    screen.Input.Kind == "text" ? (++typed == 1 ? "ViralTester" : $"ViralTester{typed}") : null, screen.Input.Kind == "text" ? null : 0);
            else
            {
                var choices = screen.Actions.Where(a => a.Enabled).ToArray();
                var chosen = choices.FirstOrDefault(a => a.VehicleId == "minivan") ??
                    choices.FirstOrDefault(a => a.Label.Contains("travel light", StringComparison.OrdinalIgnoreCase)) ??
                    choices.FirstOrDefault();
                if (chosen != null) await Accepted(engine, chosen.ActionId);
            }
        }
        throw new Exception($"Setup did not reach travel: {engine.Snapshot.Screen.Id}");
    }

    // A day of filming may raise an ordinary road event above the studio.
    private static async Task BackToStudio(GameEngine engine)
    {
        for (var i = 0; i < 15; i++)
        {
            await Settle(engine);
            if (engine.Snapshot.Creator != null) return;
            Check(engine.Snapshot.Screen.Kind != "game-over", "test journey ended unexpectedly");
            var action = engine.Snapshot.Screen.Actions.FirstOrDefault(a => a.Enabled);
            if (action != null) await Act(engine, action.ActionId);
        }
        throw new Exception("Studio did not return after the filming day");
    }

    private static async Task Journey(bool accept)
    {
        GameSimulationApp_Guard();
        using var engine = new GameEngine();
        await engine.InitializeAsync(default);
        await PrepareTrip(engine);
        await Accepted(engine, "creator.open");
        await Settle(engine);
        await Accepted(engine, "creator.start", accept ? "Sold Out" : "Kept It");

        // Good-idea pool and first idea, then the publish draws: breakout no, viral yes, interest .5,
        // regulars, conversion gate, conversion, churn, discovery noise, limited ads (accept: flagged), RPM $5.
        var career = Career(engine);
        career.Random = new SequenceRandom(0);
        await Accepted(engine, "creator.film");
        await BackToStudio(engine);
        career = Career(engine);
        career.Random = new SequenceRandom(.5, 0, .5, .5, .5, .5, .5, .5, accept ? .1 : .99, .5);
        var turns = engine.Snapshot.Hud.Turns;
        var cash = engine.Snapshot.Hud.Balance;
        var publishRevision = engine.Snapshot.Revision;
        await Accepted(engine, "creator.publish");

        var pending = engine.Snapshot;
        var offer = pending.Creator.License;
        var video = pending.Creator.Videos[0];
        Check(offer != null && video.Viral && offer.VideoId == video.Id && video.Views == 400000 && offer.Offer == 1600,
            "forced viral publish opens a $1,600 offer for 400,000 views past the phone ceiling");
        Check(pending.Screen.Actions.Select(a => a.ActionId).OrderBy(a => a)
            .SequenceEqual(new[] { "creator.license-accept", "creator.license-decline" }), "only the two answers are advertised");
        Check(video.Rpm == 0 && video.Revenue == 0 && pending.Creator.Stats.Revenue == 0 && career.License.Rpm == 5m,
            "held RPM and ad money stay on the server");
        using (var json = JsonDocument.Parse(JsonSerializer.Serialize(pending, Web)))
        {
            var license = json.RootElement.GetProperty("creator").GetProperty("license");
            Check(!license.EnumerateObject().Any(p => p.Name is "rpm" or "revenue" or "potential"),
                "serialized offer carries no held RPM, revenue, or potential");
            Check(json.RootElement.GetProperty("creator").GetProperty("videos")[0].GetProperty("rpm").GetDecimal() == 0,
                "serialized pending video shows no RPM");
        }
        Check(video.Demonetized == accept, "limited-ads result is visible before the decision");

        foreach (var id in new[] { "creator.leave", "creator.film", "creator.withdraw", "creator.tab.shop", "creator.reedit.1" })
            await Rejected(engine, id, "invalid-action");
        var stale = await engine.DispatchAsync(new GameActionRequest(accept ? "creator.license-accept" : "creator.license-decline",
            publishRevision, null, null), default);
        Check(!stale.Accepted && stale.ErrorCode == "stale-revision", "an answer on an old revision is stale");

        var answer = accept ? "creator.license-accept" : "creator.license-decline";
        var answerRevision = engine.Snapshot.Revision;
        await Accepted(engine, answer);
        var settled = engine.Snapshot;
        var replay = await engine.DispatchAsync(new GameActionRequest(answer, answerRevision, null, null), default);
        Check(!replay.Accepted && replay.ErrorCode == "stale-revision", "replaying the answer is stale");
        await Rejected(engine, "creator.license-accept", "invalid-action");
        await Rejected(engine, "creator.license-decline", "invalid-action");
        Check(settled.Creator.License == null && settled.Hud.Turns == turns, "answering spends no trail day");
        Check(settled.Screen.Actions.Any(a => a.ActionId == "creator.leave"), "studio reopens after the answer");
        var answered = settled.Creator.Videos[0];
        if (accept)
        {
            Check(settled.Hud.Balance == cash + 1600 && settled.Creator.Stats.LicenseIncome == 1600 &&
                settled.Creator.Stats.Revenue == 0 && answered.Licensed && answered.LicensePaid == 1600,
                "selling pays road cash once and no ad money");
            Check(!settled.Screen.Actions.Any(a => a.ActionId.StartsWith("creator.reedit.")), "a sold video offers no re-edit");
            Check(answered.Feedback.Contains("re-edit"), "a sold limited-ads video says the re-edit went with it");
            await Rejected(engine, $"creator.reedit.{answered.Id}", "invalid-action");
        }
        else
        {
            Check(settled.Hud.Balance == cash && settled.Creator.Stats.LicenseIncome == 0 &&
                settled.Creator.Stats.Revenue == 2000 && answered.Revenue == 2000 && answered.Rpm == 5m && !answered.Licensed,
                "keeping releases the held ads to the channel balance once");
            await Accepted(engine, "creator.withdraw");
            Check(engine.Snapshot.Hud.Balance == cash + 2000, "the kept video's ads reach road cash only by withdrawal");
        }
        await Accepted(engine, "creator.leave");
        await Settle(engine);
        Check(engine.Snapshot.Creator == null && engine.Snapshot.Hud.Turns == turns, "player can leave the studio afterwards");
    }

    private static void GameSimulationApp_Guard() =>
        Check(OregonTrailDotNet.GameSimulationApp.Instance == null, "no journey context leaks between tests");
}
