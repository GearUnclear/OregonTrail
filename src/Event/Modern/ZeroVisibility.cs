// The Asphalt Trail (2028 re-skin) -- PILEUP (whole-party catastrophe).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the collective fiction that everyone can drive full speed into a wall of nothing and it will
    ///     work out. Grounded in the recurring Central Valley tule-fog chain-reaction pileups and the March 2022
    ///     I-81 Pennsylvania pileup (dozens of vehicles, multiple dead) -- visibility drops to zero and the traffic
    ///     behind does not.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class ZeroVisibility : ModernPileup
    {
        /// <summary>The flavor line describing the specific multi-car catastrophe.</summary>
        protected override string OnPileup()
        {
            return "The fog bank swallows the interstate between one mile marker and the next -- a" +
                   " gray wall with no depth to it. Nobody behind you lifts off the gas, because " +
                   "nobody behind you can see that you have. The impacts arrive as sound first, " +
                   "then as physics, forty vehicles folding into each other in the white.";
        }
    }
}
