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
            return "The rig rolls once and settles against the guardrail, smoking but intact -- everyone conscious, " +
                   "everyone belted, everyone fine. Then the twelve-volt line burns through and the door buttons go " +
                   "dark. The manual releases are under the seat map-pockets where the manual said they were. Outside, " +
                   "a passerby with a tire iron cannot get the stainless panels open, and the touchscreen keeps " +
                   "cheerfully offering to play everyone's favorite podcast.";
        }
    }
}
