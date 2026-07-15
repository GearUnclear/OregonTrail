// The Asphalt Trail (2028 re-skin) -- SOLO_MAIM (one badly hurt + injured).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the algorithm rewards the stunt right up until the stunt collects. Grounded in the documented
    ///     genre of viral-challenge and "hold my beer" trauma -- rooftop jumps, moving-car surfing, parking-lot
    ///     backflips filmed for a platform that pays in views and never in medical bills.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class HoldMyBeer : ModernSoloMaim
    {
        /// <summary>Describes how this particular passenger was maimed.</summary>
        protected override string OnMaim(Entity.Person.Person victim)
        {
            return "There is a trending sound and a flat stretch of motel roof, and\n" +
                   $"{victim.Name} has a phone propped on the ice machine and a plan. The\n" +
                   "takeoff is clean. The landing is filed under 'content warning.' The\n" +
                   "clip does numbers; the ankle, the wrist, and one vertebra do not.";
        }
    }
}
