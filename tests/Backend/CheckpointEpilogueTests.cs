using OregonTrailDotNet.Module.Choices;
using OregonTrailDotNet.Window.Travel.Decision;

internal static class CheckpointEpilogueTests
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    // Mirrors how the checkpoint "run" option reads the earlier caravan decision from the ledger.
    private static string RunAfter(string caravanOption)
    {
        var ledger = new ChoiceLedger();
        if (caravanOption != null) ledger.Record("caravan", caravanOption, 0, null);
        ledger.Record("checkpoint", "run", 0, CheckpointDecision.RunEpilogue(ledger.GetDecision("caravan")));
        return ledger.Epilogue[^1];
    }

    public static void Run()
    {
        var joined = RunAfter("join");
        var solo = RunAfter("solo");
        var never = RunAfter(null);

        Check(joined.Contains("Forty families who had vouched for you"), "joined run recap names the abandoned caravan");
        Check(!solo.Contains("families") && !solo.Contains("caravan"), "solo run recap must not mention a caravan");
        Check(solo.Contains("going it alone"), "solo run recap reflects traveling alone");
        Check(!never.Contains("families") && !never.Contains("caravan"), "unoffered run recap must not mention a caravan");
        Check(joined != solo && solo != never && joined != never, "each caravan outcome gets its own recap");
        foreach (var line in new[] { joined, solo, never })
        {
            Check(line.StartsWith("You took a logging road around the Cascadia line"), "run recap keeps its opening");
            Check(!line.Contains("If the caravan"), "run recap no longer hedges on the caravan");
        }
        Console.WriteLine("PASS CheckpointEpilogueTests");
    }
}
