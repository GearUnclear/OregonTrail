// The Asphalt Trail (2028 re-skin) -- SOLO_KILL (one satirical death).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the lie of the safe crowd -- that ten thousand bodies optimized for one stage will make room for
    ///     yours, and that someone in a vest is in charge. Grounded in the 2021 Astroworld crowd crush (ten dead,
    ///     ages 9-27, compression asphyxia) at a barrier-free general-admission show.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class GeneralAdmission : ModernSoloDeath
    {
        /// <summary>Describes how this particular passenger met their end.</summary>
        protected override string OnDeath(Entity.Person.Person victim)
        {
            return "The convoy detours into a sold-out roadside festival because the\n" +
                   "tickets were free and the headliner is trending. The floor has no\n" +
                   "barriers and no aisles, just forward. When the beat drops the\n" +
                   $"crowd inhales as one organism and {victim.Name}'s feet leave the\n" +
                   "ground without permission -- upright, eyes open, and already gone,\n" +
                   "held vertical by ten thousand people there to have a good time.";
        }
    }
}
