// The Asphalt Trail (2028 re-skin) -- SOLO_MAIM (one badly hurt + injured).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the universal private belief that you, specifically, can text and drive. Grounded in NHTSA
    ///     distracted-driving fatality counts (3,000+/year) and the "just for a second" glance that is statistically
    ///     several car lengths at highway speed.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class EyesOffJustForASecond : ModernSoloMaim
    {
        /// <summary>Describes how this particular passenger was maimed.</summary>
        protected override string OnMaim(Entity.Person.Person victim)
        {
            return $"{victim.Name} looks down to change the song -- just for a second, the\n" +
                   "way it is always just for a second. The convoy drifts onto the rumble strip,\n" +
                   "overcorrects, and clips the guardrail hard enough to rearrange the front end\n" +
                   "and everyone's neck. The song, for the record, was fine.";
        }
    }
}
