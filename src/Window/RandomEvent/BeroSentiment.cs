// The Asphalt Trail (2028 re-skin) -- sentiment scorer for the Tom Holland / BERO encounter.

using System;
using System.Collections.Generic;

namespace OregonTrailDotNet.Window.RandomEvent
{
    /// <summary>
    ///     How the scorer read the player's attempted reply. Tom does not actually listen -- he talks over the
    ///     player and then "hears" whatever he wants -- but the encounter form uses this to pick which thing he
    ///     decides you said. <see cref="Neutral" /> collapses into acceptance downstream because a man handing out
    ///     his own beer assumes a yes by default.
    /// </summary>
    public enum BeroLean
    {
        /// <summary>The player's words skewed toward accepting / engaging with the pitch.</summary>
        Accept,

        /// <summary>The player's words skewed toward refusing / recoiling from the pitch.</summary>
        Reject,

        /// <summary>No clear signal either way; the player mumbled or said something off-topic.</summary>
        Neutral
    }

    /// <summary>
    ///     A deliberately BASIC bag-of-words sentiment scorer. It is not meant to be clever -- it counts how many
    ///     accept-leaning versus reject-leaning tokens the player managed to get out before Tom steamrolled them,
    ///     then returns the winning lean. The comedy is that Tom ignores the result anyway.
    ///
    ///     Input constraint this must live with: WolfCurses' InputManager only feeds LETTERS AND DIGITS into the
    ///     buffer -- spaces, apostrophes, and punctuation are silently dropped (see EpitaphEditor's note). So the
    ///     player commits one bare word per ENTER and contractions arrive stripped: "don't" -> "dont",
    ///     "won't" -> "wont". The lexicons below are spelled to match that stripped form on purpose.
    /// </summary>
    public static class BeroSentiment
    {
        /// <summary>
        ///     Words that read as the player leaning INTO the pitch -- agreement, enthusiasm, or just naming the
        ///     product back at him (Tom takes any mention of BERO/beer/drink as a buying signal).
        /// </summary>
        private static readonly HashSet<string> AcceptWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "yes", "yeah", "yep", "yup", "yea", "ok", "okay", "sure", "fine", "alright", "aight",
            "love", "like", "great", "good", "nice", "cool", "awesome", "amazing", "delicious", "tasty",
            "please", "thanks", "thank", "cheers", "deal", "absolutely", "definitely", "totally", "gladly",
            "want", "gimme", "give", "take", "ill", "down", "sounds", "sold", "one", "two", "case", "sixpack",
            "bero", "beer", "drink", "sip", "taste", "try", "sample", "refreshing", "wow", "cool", "based"
        };

        /// <summary>
        ///     Words that read as the player recoiling -- refusal, disgust, or telling him to get lost. Contractions
        ///     are spelled without the apostrophe the input layer strips ("dont", "wont", "cant").
        /// </summary>
        private static readonly HashSet<string> RejectWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "no", "nope", "nah", "naw", "never", "not", "dont", "wont", "cant", "stop", "quit", "pass",
            "leave", "away", "go", "off", "back", "refuse", "decline", "hate", "gross", "disgusting", "nasty",
            "yuck", "ew", "eww", "sick", "weird", "creepy", "scram", "loser", "smoke", "cigarette", "sober",
            "water", "help", "hater"
        };

        /// <summary>
        ///     Scores the words the player managed to type against the two lexicons and returns which side won.
        /// </summary>
        /// <param name="words">Every bare word the player committed across the exchange, in any order.</param>
        /// <returns>The player's overall lean; ties and empty input come back <see cref="BeroLean.Neutral" />.</returns>
        public static BeroLean Score(IEnumerable<string> words)
        {
            if (words == null)
                return BeroLean.Neutral;

            var accept = 0;
            var reject = 0;
            foreach (var raw in words)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                var word = raw.Trim();
                if (AcceptWords.Contains(word))
                    accept++;
                if (RejectWords.Contains(word))
                    reject++;
            }

            if (reject > accept)
                return BeroLean.Reject;
            if (accept > reject)
                return BeroLean.Accept;
            return BeroLean.Neutral;
        }
    }
}
