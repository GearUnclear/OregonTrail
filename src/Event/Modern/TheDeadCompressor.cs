// The Asphalt Trail (2028 re-skin) -- PILEUP (whole-party catastrophe).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the American faith that the grid and the machine will always save you from a planet you helped
    ///     overheat. Grounded in record U.S. heat deaths (2,300+ in 2023, higher in 2024) and the finding that a
    ///     large share happen INDOORS with a broken or powered-off AC unit present -- plus the 2021 Pacific Northwest
    ///     heat dome (hundreds dead, the overwhelming majority indoors).
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheDeadCompressor : ModernPileup
    {
        /// <summary>The flavor line describing the specific multi-car catastrophe.</summary>
        protected override string OnPileup()
        {
            return "Gridlock on the interstate, 118 degrees, the third day of the heat dome. The compressor clicks, " +
                   "hesitates, and dies, and the cabin becomes an oven with the windows already down. The dashboard " +
                   "keeps insisting the climate system is ON. Help is four exits away in traffic that has not moved in " +
                   "an hour.";
        }
    }
}
