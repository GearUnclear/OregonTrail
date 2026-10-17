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

        // Typed names: a name already in the party is refused regardless of case, spacing or dropped punctuation
        // (the console buffer keeps only letters and digits); blank input still autogenerates.
        var party = new[] { "Viva", " Mary  Ann " };
        foreach (var duplicate in new[] { "Viva", "viva", "  VIVA  ", "\tViva\n", "Vi va", "Viva!", "mary ann", "MaryAnn", "mary-ann" })
            if (!InputPlayerNames.IsNameTaken(party, duplicate)) throw new Exception($"Accepted duplicate '{duplicate}'");
        foreach (var distinct in new[] { "Vivian", "Viv", "Mary", "Ann", "Viva2" })
            if (InputPlayerNames.IsNameTaken(party, distinct)) throw new Exception($"Refused distinct name '{distinct}'");
        foreach (var blank in new[] { "", "   ", null })
            if (InputPlayerNames.IsNameTaken(new[] { "", " " }, blank)) throw new Exception("Blank input must stay autogenerate");
        if (InputPlayerNames.IsNameTaken(Array.Empty<string>(), "Viva")) throw new Exception("Empty party refused a name");
        var travelers = new List<string> { "traveler1" };
        for (var i = 0; i < 26; i++) travelers.Add(InputPlayerNames.GetPlayerName(travelers, new Random(0)));
        if (InputPlayerNames.GetPlayerName(travelers, new Random(0)) != "Traveler 2")
            throw new Exception("Fallback names must skip ones already typed without spaces");
        Console.WriteLine("PASS 1000 seeded automatic and partial parties, case/whitespace exclusion, catalog exhaustion, typed duplicates");
    }
}
