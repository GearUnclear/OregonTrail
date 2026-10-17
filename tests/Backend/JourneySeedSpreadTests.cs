using System.Diagnostics;
using System.Reflection;
using OregonTrailDotNet;
using OregonTrailDotNet.Module.Crypto;
using OregonTrailDotNet.Window.Travel;
using OregonTrailDotNet.Window.Travel.Creator;
using OregonTrailDotNet.Window.Travel.Crypto;
using WolfCurses.Window;

/// <summary>
/// Issue #71: unseeded journeys must not share a handful of random streams. WolfCurses seeds every journey with
/// <c>(int)DateTime.Now.Ticks &amp; 0xFFF</c> (4096 streams), and its no-argument <c>Next()</c> is <c>Next(60)</c>,
/// so desks seeded from it had only 60 creator careers and 60 crypto markets.
/// </summary>
internal static class JourneySeedSpreadTests
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo Windows = typeof(WolfCurses.Core.WindowManager).GetField("_windowList", Hidden)!;
    private static readonly FieldInfo ExchangeRandom = typeof(CryptoExchange).GetField("_random", Hidden)!;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Journey seed spread: " + message);
    }

    public static void Run()
    {
        Check(GameSimulationApp.CanSeedJourneys, "the pinned WolfCurses still exposes the generator UseSeed replaces");
        var clock = Stopwatch.StartNew();

        // More journeys than WolfCurses' 4096 clock seeds, created back to back so many share a clock tick.
        const int journeys = 5000;
        var firstDraws = new HashSet<int>();
        var noArgumentMax = 0;
        for (var i = 0; i < journeys; i++)
            WithJourney(game =>
            {
                firstDraws.Add(game.Random.Next(int.MaxValue));
                noArgumentMax = Math.Max(noArgumentMax, game.Random.Next());
            });
        Check(noArgumentMax < 60, $"premise: WolfCurses' Next() is Next(60) (saw {noArgumentMax})");
        Check(firstDraws.Count > 4096, $"{journeys} unseeded journeys use more than 4096 streams (got {firstDraws.Count})");

        // Creator careers keep the generator their first desk opening gives them, so compare across journeys.
        const int desks = 200;
        var creators = new HashSet<double>();
        var markets = new HashSet<double>();
        for (var i = 0; i < desks; i++)
            WithJourney(game =>
            {
                game.Restart();
                var travel = TravelWindow(game);
                travel.SetForm(typeof(CreatorDesk));
                creators.Add(((CreatorDesk)travel.CurrentForm).Channel.Career.Random.NextDouble());
                travel.SetForm(typeof(CryptoDesk));
                markets.Add(((Random)ExchangeRandom.GetValue(((CryptoDesk)travel.CurrentForm).Exchange)!).NextDouble());
            });
        Check(creators.Count > 60, $"{desks} journeys open more than 60 distinct creator careers (got {creators.Count})");
        Check(markets.Count > 60, $"{desks} journeys open more than 60 distinct crypto markets (got {markets.Count})");

        // Each crypto desk opening draws a new market, so one journey must not cycle through 60 of them either.
        var reopened = new HashSet<double>();
        WithJourney(game =>
        {
            game.Restart();
            var travel = TravelWindow(game);
            for (var i = 0; i < desks; i++)
            {
                travel.SetForm(typeof(CryptoDesk));
                reopened.Add(((Random)ExchangeRandom.GetValue(((CryptoDesk)travel.CurrentForm).Exchange)!).NextDouble());
                travel.ClearForm();
            }
        });
        Check(reopened.Count > 60, $"{desks} crypto desk openings in one journey give more than 60 markets (got {reopened.Count})");
        Check(GameSimulationApp.Instance == null, "no journey context leaks");
        Console.WriteLine($"PASS unseeded journey spread: {firstDraws.Count}/{journeys} journey streams, " +
            $"{creators.Count}/{desks} creator careers, {markets.Count}/{desks} crypto markets, " +
            $"{reopened.Count}/{desks} reopened markets ({clock.Elapsed.TotalSeconds:0.0}s)");
    }

    private static void WithJourney(Action<GameSimulationApp> body)
    {
        GameSimulationApp.Activate(null);
        GameSimulationApp.Create();
        try { body(GameSimulationApp.Instance); }
        finally
        {
            GameSimulationApp.Instance?.Destroy();
            GameSimulationApp.Activate(null);
        }
    }

    private static IWindow TravelWindow(GameSimulationApp game) =>
        ((Dictionary<Type, IWindow>)Windows.GetValue(game.WindowManager)!)[typeof(Travel)];
}
