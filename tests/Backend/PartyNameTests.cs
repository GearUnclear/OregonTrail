using OregonTrailDotNet.Window.MainMenu.Names;

internal static class PartyNameTests
{
    public static void Run()
    {
        for (var seed = 0; seed < 1000; seed++)
        {
            foreach (var initial in new[] { Array.Empty<string>(), new[] { " bob ", "SALLY" } })
            {
                var names = initial.ToList();
                var random = new Random(seed);
                while (names.Count < 4) names.Add(InputPlayerNames.GetPlayerName(names, random));
                if (names.Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
                    throw new Exception("Generated duplicate party names");
                if (!names.Take(initial.Length).SequenceEqual(initial)) throw new Exception("Changed entered names");
            }
        }
        var exhausted = new List<string>();
        for (var i = 0; i < 30; i++) exhausted.Add(InputPlayerNames.GetPlayerName(exhausted, new Random(0)));
        if (exhausted.Distinct().Count() != 30) throw new Exception("Exhausted catalog repeats names");
        Console.WriteLine("PASS 1000 seeded automatic and partial parties, case/whitespace exclusion, catalog exhaustion");
    }
}
