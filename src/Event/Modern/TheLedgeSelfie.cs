// The Asphalt Trail (2028 re-skin) -- SOLO_KILL (one satirical death).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: optimizing your life for an audience that will not attend the funeral. Grounded in the
    ///     documented epidemic of selfie deaths -- 400+ recorded worldwide since 2008, roughly half from falls -- and
    ///     specifically the 2024 waterfall-reel deaths of travel influencers who stepped back for the shot.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheLedgeSelfie : ModernSoloDeath
    {
        /// <summary>Describes how this particular passenger met their end.</summary>
        protected override string OnDeath(Entity.Person.Person victim)
        {
            return $"{victim.Name} backs toward the canyon rim to fit the whole waterfall " +
                   "into frame, one thumb hunting for the record button, eyes on the " +
                   "little rectangle instead of the ground. The convoy hears the shutter " +
                   "sound, then the scream, then nothing. The phone is recovered forty " +
                   "feet down, intact, still shooting vertical video of the sky.";
        }
    }
}
