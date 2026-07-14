// The Asphalt Trail (2028 re-skin) -- SOLO_MAIM (one badly hurt + injured).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: optimizing your body with podcast biohacks -- dopamine, "cortisol," a protocol for everything --
    ///     administered with zero medical supervision at a rest-stop reservoir. Grounded in the cold-plunge/Wim-Hof
    ///     trend and documented cold-water-shock and shallow-water-blackout incidents among enthusiasts chasing the
    ///     morning routine.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheColdPlunge : ModernSoloMaim
    {
        /// <summary>Describes how this particular passenger was maimed.</summary>
        protected override string OnMaim(Entity.Person.Person victim)
        {
            return $"The podcast was very clear about the cortisol benefits, so {victim.Name} wades into the snowmelt " +
                   "reservoir behind the rest area for a two-minute cold plunge and a breathing protocol learned from a " +
                   "reel. Cold-water shock does not care about the protocol; the gasp reflex fires underwater. They are " +
                   "dragged out in time, but not soon enough to come away undamaged.";
        }
    }
}
