using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using OregonTrailDotNet;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Web;
using WolfCurses.Window.Form;

/// <summary>
///     Issue #40: WolfCurses discovers [ParentWindow] forms by scanning Assembly.GetEntryAssembly(). In this harness
///     the entry assembly is OregonTrail.BackendTests, so a real journey that reached its first one-second tick
///     (Restart -> Travel.OnWindowPostCreate -> SetForm) failed with "State factory cannot create state from type that
///     does not exist in reference states". It was intermittent only because the harness usually disposes its real
///     workers before that first tick. The game must register its own forms and events whatever the host is.
/// </summary>
internal static class StateRegistryTests
{
    static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("State registry: " + message);
    }

    public static async Task Run()
    {
        var game = typeof(GameSimulationApp).Assembly;
        // The bug only exists when the game is hosted by another executable, which is exactly this harness.
        Check(Assembly.GetEntryAssembly() != game, "the harness must not be the game's entry assembly");

        var forms = game.DefinedTypes.Where(type => type.IsDefined(typeof(ParentWindowAttribute), false)).ToArray();
        var events = game.DefinedTypes.Where(type => !type.IsAbstract && type.IsDefined(typeof(DirectorEventAttribute), true)).ToArray();
        Check(forms.Length > 0 && events.Length > 0, "the game declares forms and events");

        GameSimulationApp.Activate(null);
        GameSimulationApp.Create();
        try
        {
            var registered = GameStateRegistry.RegisteredForms(GameSimulationApp.Instance.WindowManager);
            var missing = forms.Where(form => !registered.Contains(form)).Select(form => form.Name).ToArray();
            Check(missing.Length == 0, "forms missing from the window manager: " + string.Join(", ", missing));

            // Drive the real first tick (it fires only after one wall-clock second) and the setup window's SetForm.
            var started = DateTime.UtcNow;
            while (GameSimulationApp.Instance.WindowManager.FocusedWindow?.CurrentForm == null)
            {
                Check(DateTime.UtcNow - started < TimeSpan.FromSeconds(10), "the first tick never opened a form");
                GameSimulationApp.Instance.OnTick(true);
                Thread.Sleep(50);
            }
            Check(GameSimulationApp.Instance.WindowManager.FocusedWindow.CurrentForm.GetType().Name == "MainMenuScreen",
                "the first tick opens the main menu");

            // Events are discovered the same way; every declared event must be known to the director's factory.
            var factory = new EventFactory();
            foreach (var type in events)
                Check(factory.IsRegistered(type), "event not registered: " + type.Name);
        }
        finally
        {
            GameSimulationApp.Instance?.Destroy();
            GameSimulationApp.Activate(null);
        }

        // The production path: a real worker that outlives the first tick must publish the setup screen, not fault.
        var options = new GameHostOptions { CommandCapacity = 2, MaximumStreamsPerSession = 2 };
        await using (var worker = new JourneyWorker(options, NullLogger.Instance))
        {
            var initial = await worker.ReadAsync(default).WaitAsync(TimeSpan.FromSeconds(10));
            Check(!worker.IsFaulted, "a real journey worker survives its first tick");
            Check(initial.State.Screen.Kind == "setup" && initial.State.Screen.Actions.Count > 0,
                "a real journey worker publishes the setup screen");
        }
        Check(GameSimulationApp.Instance == null, "the worker context does not leak");
        Console.WriteLine("PASS forms and events register without the game as the entry assembly");
    }
}
