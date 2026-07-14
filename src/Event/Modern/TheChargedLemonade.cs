// The Asphalt Trail (2028 re-skin) -- SOLO_KILL (one satirical death).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: consumer-grade stimulant maximalism -- a legal drug sold as refreshment, lethal dose hidden
    ///     behind a wholesome brand and a free-refill lever. Grounded in the charged-lemonade deaths (a 21-year-old
    ///     with long-QT in 2022, a 46-year-old in 2023) that pulled the ~390mg fountain drink from a bread-bowl chain
    ///     in 2024, and the longer body count of bulk-caffeine overdose.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheChargedLemonade : ModernSoloDeath
    {
        /// <summary>Describes how this particular passenger met their end.</summary>
        protected override string OnDeath(Entity.Person.Person victim)
        {
            return $"At the highway bread-bowl franchise, {victim.Name} fills up free-refill on the neon 'Electrolyte " +
                   "Lemonade' -- same dispenser as the regular stuff, four cups before the on-ramp. Somewhere past mile " +
                   "300 a heart nobody knew had a long-QT problem simply stops keeping time. The receipt lists it as a " +
                   "beverage.";
        }
    }
}
