using OregonTrailDotNet.Window.Travel.Hunt;

internal static class FoodSweepTests
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Food sweep: " + message);
    }
    public static void Run()
    {
        long now = 0;
        var sweep = new HuntManager(new Random(42), () => now);
        Check(!sweep.TryGrab(sweep.ZoneStart), "future timing is rejected");
        Check(!sweep.TryGrab(-1), "negative timing is rejected");
        now = sweep.ZoneStart;
        Check(sweep.TryGrab(sweep.ZoneStart) && sweep.KillWeight > 0, "zone edge hits");
        var food = sweep.KillWeight;
        Check(!sweep.TryGrab(sweep.ZoneStart) && sweep.KillWeight == food, "duplicate grab cannot award twice");
        now += 349;
        sweep.OnTick(true, false);
        Check(sweep.Round == 1 && sweep.Resolved, "feedback stays visible before next tray");
        now++;
        sweep.OnTick(true, false);
        Check(sweep.Round == 2 && !sweep.Resolved, "next tray arrives without input");
        Check(sweep.TryGrab(0) && sweep.KillWeight == food, "early attempt misses");
        now += 350;
        sweep.OnTick(true, false);
        now += sweep.ZoneEnd + 1;
        Check(sweep.TryGrab(sweep.ZoneEnd + 1) && sweep.KillWeight == food, "late attempt misses");
        while (!sweep.ShouldEndHunt)
        {
            now += 5200;
            sweep.OnTick(true, false);
        }
        Check(sweep.Round == 8 && sweep.KillWeight == food, "timeouts finish all trays without extra food");
        now = 0;
        sweep = new HuntManager(new Random(17), () => now);
        while (!sweep.ShouldEndHunt)
        {
            now += sweep.ZoneEnd;
            Check(sweep.TryGrab(sweep.ZoneEnd), "last millisecond in zone hits");
            now += 350;
            sweep.OnTick(true, false);
        }
        Check(sweep.KillWeight == 100 && sweep.Round <= 8, "perfect play stops at carry cap");
        Check(!sweep.TryGrab(0), "completed sweep rejects inputs");
        now = 0;
        sweep = new HuntManager(new Random(42), () => now);
        while (!sweep.ShouldEndHunt && now <= 24000)
        {
            now += 50;
            sweep.OnTick(true, false);
        }
        Check(sweep.ShouldEndHunt && sweep.Round == 8 && sweep.KillWeight == 0,
            "idle sweep finishes all eight trays within 24 seconds");
        Console.WriteLine("PASS food sweep timing, early/late misses, replay, timeout, carry cap");
    }
}
