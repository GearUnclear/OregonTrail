// The Asphalt Trail (2028 re-skin) -- PILEUP (whole-party catastrophe).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: deferred maintenance as a national policy -- tens of thousands of U.S. bridges rated "fracture
    ///     critical" or in poor condition, funded eventually, inspected occasionally. Grounded in the 2007 I-35W
    ///     Minneapolis collapse and the 2024 Francis Scott Key Bridge collapse: a structure with no redundancy fails
    ///     all at once, with whoever is on it at the time.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class FractureCritical : ModernPileup
    {
        /// <summary>The flavor line describing the specific multi-car catastrophe.</summary>
        protected override string OnPileup()
        {
            return "The bridge is posted 'FRACTURE CRITICAL -- INSPECTION PENDING,' which " +
                   "everyone reads as a formality and nobody reads as a warning. Halfway " +
                   "across, a gusset plate that has been rusting since the last " +
                   "administration lets go, and the deck stops being a bridge and becomes " +
                   "a series of falling pieces with cars on them.";
        }
    }
}
