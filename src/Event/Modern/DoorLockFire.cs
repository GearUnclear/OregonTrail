// The Asphalt Trail (2028 re-skin) -- CARNAGE (whole-party wipe).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: worshipping frictionless design until the convenience becomes the coffin. Grounded in the
    ///     Nov 2024 Piedmont, CA crash that killed three students when a stainless EV hit a tree, caught fire, and
    ///     its powered doors -- no exterior handles, manual release hidden under the seat map-pocket -- went dark with
    ///     the twelve-volt line. Wrongful-death suits allege the maker knew for a decade the door system could trap
    ///     occupants.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class DoorLockFire : ModernCarnage
    {
        /// <summary>The flavor line describing the specific catastrophe that wiped the convoy.</summary>
        protected override string OnCarnage()
        {
            return "The rig rolls once and comes to rest against the guardrail, smoking.\n" +
                   "Nobody is hurt. Then the twelve-volt line burns through and the door\n" +
                   "buttons go dark. The manual releases are behind the map pockets, per\n" +
                   "page 212 of a manual nobody opened. A passerby with a tire iron can't\n" +
                   "get through the stainless panels, and the touchscreen keeps offering\n" +
                   "to resume everyone's favorite podcast.";
        }
    }
}
