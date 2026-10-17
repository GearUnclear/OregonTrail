using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OregonTrailDotNet;
using OregonTrailDotNet.Web;

/// <summary>
/// Issue #69: a test-only seed fixes every random generator a journey owns. The same seed and the same actions must
/// replay the same journey, even when the one-second game ticks land at different moments relative to the actions.
/// </summary>
internal static class SeededJourneyTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo LastTick = typeof(WolfCurses.SimulationApp).GetField("_lastTickTime", Hidden)!;
    private static readonly FieldInfo GameField = typeof(GameEngine).GetField("_game", Hidden)!;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Seeded journey: " + message);
    }

    public static async Task Run()
    {
        Guards();
        await Sessions();

        // Same seed, different clocks: one game tick every 3 pulses versus every 7 pulses, out of phase.
        var first = await Journey(seed: 2, cadence: 3, phase: 0);
        var second = await Journey(seed: 2, cadence: 7, phase: 4);
        Check(first.Count == second.Count, $"same seed replays the same number of settled screens ({first.Count} vs {second.Count})");
        for (var step = 0; step < first.Count; step++)
            Check(first[step] == second[step], $"same seed diverged at settled screen {step}:\n{first[step]}\n{second[step]}");
        Check(first.Any(state => state.Contains("-rivercross\"")) && first.Any(state => state.Contains("talktopeople")),
            "the replay talks to a stranger and reaches the river crossing");
        var other = await Journey(seed: 3, cadence: 3, phase: 0);
        Check(!other.SequenceEqual(first), "a different seed plays a different journey");
        Check(GameSimulationApp.Instance == null, "no journey context leaks out of the seeded engines");
        Console.WriteLine($"PASS seeded journeys: guards, new-journey-only seeds, and {first.Count} identical settled snapshots " +
            "for one seed under different tick timing");
    }

    private static void Guards()
    {
        TestSeeds.EnsureAllowed(false, new Environment("Production"));
        TestSeeds.EnsureAllowed(true, new Environment("Development"));
        foreach (var name in new[] { "Production", "Staging", "development-ish" })
        {
            try { TestSeeds.EnsureAllowed(true, new Environment(name)); throw new Exception($"{name} host started with test seeds"); }
            catch (InvalidOperationException) { }
        }

        Check(TestSeeds.Read(Request(), enabled: true) == null && TestSeeds.Requested(Request()) == null, "no header, no seed");
        Check(Status(TestSeeds.Read(Request("1"), enabled: false)) == 400, "a host with seeds off refuses the header");
        foreach (var bad in new[] { "", "seven", "-1", "+1", " 1", "1.5", "2147483648", "0x10" })
            Check(Status(TestSeeds.Read(Request(bad), enabled: true)) == 400, $"malformed seed '{bad}' is refused");
        var twice = Request("1");
        twice.Request.Headers.Append(TestSeeds.Header, "2");
        Check(Status(TestSeeds.Read(twice, enabled: true)) == 400, "two seed values are refused");
        var good = Request("2147483647");
        Check(TestSeeds.Read(good, enabled: true) == null && TestSeeds.Requested(good) == int.MaxValue, "a whole number is accepted");
    }

    private static async Task Sessions()
    {
        var closed = new GameSession(Options.Create(new GameHostOptions()), NullLogger<GameSession>.Instance);
        try { closed.GetOrCreate("off", 1); throw new Exception("a host with seeds off created a seeded journey"); }
        catch (GameSeedException) { }

        var open = new GameSession(Options.Create(new GameHostOptions { TestSeeds = true }), NullLogger<GameSession>.Instance);
        await open.StartAsync(default);
        try
        {
            var seeded = open.GetOrCreate("seeded", 5);
            Check(ReferenceEquals(seeded, open.GetOrCreate("seeded")), "the seeded journey keeps its session");
            try { open.GetOrCreate("seeded", 5); throw new Exception("a live journey was re-seeded"); }
            catch (GameSeedException) { }
            var plain = open.GetOrCreate("plain");
            try { open.GetOrCreate("plain", 5); throw new Exception("an unseeded journey was seeded"); }
            catch (GameSeedException) { }
            Check(!ReferenceEquals(seeded, plain), "sessions stay separate");
        }
        finally
        {
            await open.StopAsync(default);
            open.Dispose();
            closed.Dispose();
        }
    }

    /// <summary>
    /// Plays the policy tests/seeded_integration.py uses for its river scenario, plus one chat at Cape Coral, and
    /// returns every settled snapshot without the frame counter. The host's real one-second gate is replaced by a
    /// pulse counter so the run is fast and its tick timing is chosen by the test.
    /// </summary>
    private static async Task<List<string>> Journey(int seed, int cadence, int phase)
    {
        using var engine = new GameEngine(seed);
        await engine.InitializeAsync(default);
        var clock = new Clock(engine, cadence, phase);
        var trace = new List<string>();
        var state = clock.Settle();
        bool talked = false, forded = false;
        for (var step = 0; step < 150; step++)
        {
            trace.Add(Semantic(state));
            var screen = state.Screen;
            var id = screen.Id;
            if (forded && (id.EndsWith("locationdepart") || screen.Kind == "game-over")) return trace;
            // Some seeds break the car down before the river; that is still a complete, comparable journey.
            if (id.EndsWith("unabletocontinue") || screen.Kind == "game-over") return trace;
            var actions = screen.Actions.Where(a => a.Enabled).ToList();
            string Find(string suffix) => actions.First(a => a.ActionId.EndsWith(suffix)).ActionId;
            if (id.EndsWith("mainmenuscreen") || id.EndsWith("professionselector"))
                state = await clock.Act(Find(".select.0"));
            else if (id.EndsWith("vehicleselector"))
                state = await clock.Act(actions.First(a => a.VehicleId == "minivan").ActionId);
            else if (id.EndsWith("inputplayernames"))
                state = await clock.Act(screen.Input.ActionId, state.Party.Count == 0 ? "Seedy" : "");
            else if (id.EndsWith("packthecardecision"))
                state = await clock.Act(Find(".select.1"));
            else if (screen.Kind == "store")
            {
                foreach (var (item, amount) in new[] { ("gas", 10), ("snacks", 250), ("leggings", 4), ("tire", 2) })
                {
                    var row = state.Store.Rows.First(r => r.ItemId == item);
                    state = await clock.Act(row.SetActionId, value: Math.Min(amount, row.MaxQuantity));
                }
                state = await clock.Act("store.checkout");
            }
            else if (screen.Kind == "travel" && !talked && actions.Any(a => a.Label == "Talk to people"))
            {
                talked = true;
                state = await clock.Act(actions.First(a => a.Label == "Talk to people").ActionId);
            }
            else if (screen.Kind == "river" && id.EndsWith("-rivercross"))
            {
                forded = true;
                state = await clock.Act(actions.First(a => a.Label.StartsWith("GUN IT", StringComparison.OrdinalIgnoreCase)).ActionId);
            }
            else
            {
                var choices = actions.Where(a => !a.ActionId.StartsWith("creator.")).ToList();
                state = await clock.Act((choices.FirstOrDefault(a => a.Group == "drive") ?? choices.First()).ActionId);
            }
        }
        throw new Exception($"seed {seed} never finished the river crossing: {state.Screen.Id}");
    }

    private static string Semantic(GameSnapshotDto state) =>
        JsonSerializer.Serialize(state with { Revision = 0, JourneyId = null }, GameJsonContext.Default.GameSnapshotDto);

    /// <summary>Pulses an engine like JourneyWorker does, firing a game tick on every <c>cadence</c>-th pulse.</summary>
    private sealed class Clock(GameEngine engine, int cadence, int phase)
    {
        private long _pulses;

        private GameSimulationApp Simulation()
        {
            var game = GameField.GetValue(engine)!;
            return (GameSimulationApp)game.GetType().GetProperty("Simulation")!.GetValue(game);
        }

        private void Gate(bool fire)
        {
            if (Simulation() is { } simulation)
                LastTick.SetValue(simulation, fire ? DateTime.UtcNow.AddSeconds(-2) : DateTime.UtcNow);
        }

        private GameSnapshotDto Pulse()
        {
            Gate(_pulses++ % cadence == phase);
            return engine.Tick();
        }

        public async Task<GameSnapshotDto> Act(string actionId, string text = null, int? value = null)
        {
            Gate(fire: false);
            var result = await engine.DispatchAsync(new GameActionRequest(actionId, engine.Snapshot.Revision, text, value), default);
            Check(result.Accepted, $"{actionId} accepted: {result.ErrorCode} {result.Message}");
            return Settle();
        }

        /// <summary>The same readiness rule as the HTTP test: parked, not a timed screen, or quiet for three game ticks.</summary>
        public GameSnapshotDto Settle()
        {
            string last = null;
            var quiet = 0;
            for (var pulse = 0; pulse < 20000; pulse++)
            {
                var ticks = Simulation()?.FixedTicks;
                var state = Pulse();
                if (pulse < 5 || state.Driving?.IsDriving == true) continue;
                if (state.Screen.Kind != "activity" && state.Screen.Actions.Any(a => a.Enabled)) return state;
                var current = Semantic(state);
                if (current != last) (last, quiet) = (current, 0);
                else if (ticks != Simulation()?.FixedTicks && ++quiet >= 3) return state;
            }
            throw new Exception($"the journey never settled: {engine.Snapshot.Screen.Id}");
        }
    }

    private static HttpContext Request(string seed = null)
    {
        var context = new DefaultHttpContext();
        if (seed != null) context.Request.Headers[TestSeeds.Header] = seed;
        return context;
    }

    private static int? Status(IResult result) => (result as IStatusCodeHttpResult)?.StatusCode;

    private sealed class Environment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "OregonTrailDotNet";
        public string ContentRootPath { get; set; } = "/";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
