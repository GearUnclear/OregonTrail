// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

namespace OregonTrailDotNet.Window.Travel.Hunt.Help
{
    /// <summary>
    ///     Flavor text for the physical cost of coming up empty in the food sweep. The crowd does not apologize and does
    ///     not slow down, so every tray the player loses -- whether another shopper took it first or they fumbled the
    ///     grab themselves -- pulls one of these to tack onto the message. Purely cosmetic; nobody is actually hurt.
    /// </summary>
    internal static class ScrambleMishap
    {
        /// <summary>
        ///     Every line is pre-wrapped to fit the eighty column console and is meant to read as a complete sentence on
        ///     the end of whichever failure message pulled it.
        /// </summary>
        private static readonly string[] Mishaps =
        {
            "You get shoved to the ground and\nsomebody walks across your hand.",
            "Somebody slams your face into a\nfreezer door on the way past.",
            "An elbow catches you in the ribs and\nthe floor comes up fast.",
            "You go down under the crowd and get\ndragged a few feet by your own coat.",
            "A man twice your size puts a shoulder\nin your chest and keeps walking.",
            "You take a cart to the shins and fold\nup against the endcap.",
            "Someone grabs your collar, pulls you\nback, and steps over you.",
            "Your knee hits the tile and a\nstranger's boot finds your fingers."
        };

        /// <summary>
        ///     Selects a random mishap to describe how the player ended up on the floor this time.
        /// </summary>
        /// <returns>A pre-wrapped sentence describing the beating.</returns>
        internal static string RandomMishap()
        {
            return Mishaps[GameSimulationApp.Instance.Random.Next(Mishaps.Length)];
        }
    }
}
