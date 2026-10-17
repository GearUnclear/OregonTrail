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
        // Rejection messages: duplicates name the existing traveler; text with no letters or digits would store an empty
        // name, so it is refused; blank input is never refused because it autogenerates the rest of the party.
        if (InputPlayerNames.RejectionFor(party, " viva ") != "Viva is already in the party. Choose a different name.")
            throw new Exception("Duplicate rejection must name the existing traveler");
        foreach (var empty in new[] { "!!!", " - ", "?.,", "_" })
            if (InputPlayerNames.RejectionFor(party, empty) != "Names need at least one letter or number.")
                throw new Exception($"Accepted name without letters or digits '{empty}'");
        foreach (var blank in new[] { "", "   ", "\t", null })
            if (InputPlayerNames.RejectionFor(party, blank) != null) throw new Exception("Blank input must stay autogenerate");
        foreach (var usable in new[] { "Iris", "Mary-Jo", "R2", "José", "Jo-Ann", "D'Angelo", "O\u2019Neil", "J. R.", "Ann Marie", "Zoë-Lyn" })
            if (InputPlayerNames.RejectionFor(party, usable) != null) throw new Exception($"Refused usable name '{usable}'");
        // Letters in any script count, including ones outside the BMP and combining marks that follow a letter.
        foreach (var script in new[]
                 {
                     "Mary Elizabeth Fitzgerald", "प्रिया", "\U00020BB7一", "Nguyễn", "2Pac",
                 })
            if (InputPlayerNames.RejectionFor(party, script) != null) throw new Exception($"Refused usable name '{script}'");
        // Control and format characters (zero-width joiners and spaces, bidirectional overrides), unpaired surrogates and
        // combining marks with no letter to sit on are refused rather than stored invisibly.
        foreach (var hidden in new[]
                 {
                     "Bo‍B", "‮Bob", "Bob‮", "Ann​Marie", "Iris⁦", "Bob﻿", "Bo\uD800b", "Bo ́",
                     "Bo-́", "Al­",
                 })
            if (InputPlayerNames.RejectionFor(party, hidden) != "Names can use letters, numbers, spaces, hyphens, apostrophes and periods.")
                throw new Exception($"Accepted name with hidden characters '{hidden}'");
        // The first character becomes the party rail avatar, so it must be a letter or number.
        foreach (var leading in new[] { "-Bo", "'Ike", ".Al", " - Bo" })
            if (InputPlayerNames.RejectionFor(party, leading) != "Names must start with a letter or number.")
                throw new Exception($"Accepted name starting with punctuation '{leading}'");
        foreach (var trailing in new[] { "Bo-", "Jr.", "Ike'" })
            if (InputPlayerNames.RejectionFor(party, trailing) != null) throw new Exception($"Refused trailing punctuation '{trailing}'");

        // Browser names keep their spacing and name punctuation; only the ends are trimmed and inner runs collapsed.
        foreach (var (typed, stored) in new[]
                 {
                     ("Mary Ann", "Mary Ann"), ("  Mary   Ann  ", "Mary Ann"), ("Jo-Ann", "Jo-Ann"), ("D'Angelo", "D'Angelo"),
                     ("O\u2019Neil", "O'Neil"), ("\tJ.  R.\n", "J. R."), ("Jos\u0065\u0301", "José"), ("Ann\u00a0Marie", "Ann Marie"),
                 })
            if (InputPlayerNames.NormalizeName(typed) != stored)
                throw new Exception($"Normalized '{typed}' to '{InputPlayerNames.NormalizeName(typed)}', expected '{stored}'");
        foreach (var odd in new[] { "Mary-Jo!", "<b>Bo</b>", "Ann_Marie", "Bob\u0007", "Al & Bo", "Iris \U0001F697" })
            if (InputPlayerNames.RejectionFor(party, odd) != "Names can use letters, numbers, spaces, hyphens, apostrophes and periods.")
                throw new Exception($"Accepted name with unsupported characters '{odd}'");
        var longest = new string('a', InputPlayerNames.NameMaxLength);
        if (InputPlayerNames.RejectionFor(party, $"  {longest}  ") != null) throw new Exception("Trimmed spaces count toward length");
        var padded = $"{new string(' ', 40)}Bea{new string(' ', 30)}Lou{new string(' ', 40)}";
        if (padded.Length > InputPlayerNames.RawNameMaxLength || InputPlayerNames.RejectionFor(party, padded) != null)
            throw new Exception("Padding that normalizes away must not count toward length");
        if (InputPlayerNames.RejectionFor(party, longest + "a") != $"Names must be {InputPlayerNames.NameMaxLength} characters or fewer.")
            throw new Exception("Accepted an overlong name");
        if (InputPlayerNames.TooLongMessage != InputPlayerNames.RejectionFor(party, longest + "a"))
            throw new Exception("The raw request limit must refuse with the stored-name message");
        // Duplicates still compare letters and digits only, and the message names the traveler as they were stored.
        var spaced = new[] { "Mary Ann", "Jo-Ann" };
        foreach (var (clash, existing) in new[]
                 {
                     ("maryann", "Mary Ann"), ("Mary-Ann", "Mary Ann"), ("MARY  ANN", "Mary Ann"),
                     ("JoAnn", "Jo-Ann"), ("jo ann", "Jo-Ann"), ("Jo.Ann", "Jo-Ann"),
                 })
            if (InputPlayerNames.RejectionFor(spaced, clash) != $"{existing} is already in the party. Choose a different name.")
                throw new Exception($"Accepted duplicate '{clash}' of '{existing}'");

        // A zero-width non-joiner may sit between two letters (Persian, or after a virama in Devanagari), and nowhere else;
        // every other format character stays refused.
        const string persian = "می\u200cخواهم";
        foreach (var joined in new[] { persian, "Ali می\u200cخواهم", "क्\u200cष", "Mi\u200cna" })
            if (InputPlayerNames.RejectionFor(party, joined) != null) throw new Exception($"Refused non-joiner between letters '{joined}'");
        if (InputPlayerNames.NormalizeName($" {persian} ") != persian) throw new Exception("Normalization must keep a non-joiner");
        foreach (var misplaced in new[]
                 {
                     "\u200cمیخواهم", "میخواهم\u200c", "می\u200c\u200cخواهم", "می \u200cخواهم", "می\u200c خواهم",
                     "Bo\u200c-b", "R2\u200cD", "R\u200c2", "Ann.\u200cMarie", "می\u200c\u064e", "Bo\u200d\u200cb",
                 })
            if (InputPlayerNames.RejectionFor(party, misplaced) != "Names can use letters, numbers, spaces, hyphens, apostrophes and periods.")
                throw new Exception($"Accepted misplaced non-joiner '{misplaced}'");
        foreach (var format in new[] { "Bo\u200db", "Bo\u200eb", "Bo\u202ab", "Bo\u2066b", "Bo\u2069b", "Bo\ufeffb", "Bo\u00adb", "Bo\u2060b", "Bo\u061cb" })
            if (InputPlayerNames.RejectionFor(party, format) != "Names can use letters, numbers, spaces, hyphens, apostrophes and periods.")
                throw new Exception($"Accepted format character U+{(int)format[2]:X4}");
        // The non-joiner only shapes the letters around it, so a name with and without it is the same traveler.
        if (InputPlayerNames.RejectionFor(new[] { persian }, "میخواهم") != $"{persian} is already in the party. Choose a different name.")
            throw new Exception("A name differing only by a non-joiner must clash");
        if (InputPlayerNames.RejectionFor(new[] { "میخواهم" }, persian) == null)
            throw new Exception("A non-joiner must not make a name distinct");

        // Copy that closes a sentence on a name adds its period only when the name does not already end with one.
        foreach (var (name, sentence) in new[]
                 {
                     ("Bob", "Bob."), ("D'Angelo Jr.", "D'Angelo Jr."), ("J. R.", "J. R."), ("Ike'", "Ike'."), ("Bo-", "Bo-."),
                     (persian, persian + "."), ("", "."), (null, "."),
                 })
            if (InputPlayerNames.EndSentence(name) != sentence)
                throw new Exception($"Ended '{name}' as '{InputPlayerNames.EndSentence(name)}', expected '{sentence}'");
        if (OregonTrailDotNet.Web.PresentationCatalog.MemorialText("D'Angelo Jr.", "Drive safe", 1204) != "Here lies D'Angelo Jr.\n\nDrive safe\n\nMile 1,204.")
            throw new Exception("Tombstone doubled the period after a name ending in one");
        if (OregonTrailDotNet.Web.PresentationCatalog.MemorialText("Bob", "", 7) != "Here lies Bob.\n\n\n\nMile 7.")
            throw new Exception("Tombstone lost the period after a plain name");
        Console.WriteLine("PASS 1000 seeded automatic and partial parties, case/whitespace exclusion, catalog exhaustion, typed duplicates, empty-key names, spaced/punctuated names, hidden characters, leading punctuation, non-joiners between letters, sentence-ending names");
    }
}
