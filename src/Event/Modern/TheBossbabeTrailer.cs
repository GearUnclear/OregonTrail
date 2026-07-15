// The Asphalt Trail (2028 re-skin) -- SUPPLY_DRAIN (food).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the MLM as a machine for converting a person's savings and social capital into a garage of
    ///     product they will never sell. Grounded in the documented multilevel-marketing economy (the FTC estimates
    ///     the overwhelming majority of participants lose money) and the "bossbabe" recruiting culture that trades
    ///     groceries for inventory.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheBossbabeTrailer : ModernSupplyDrain
    {
        /// <summary>Which inventory category this drain empties.</summary>
        protected override Entities DrainCategory => Entities.Food;

        /// <summary>Inclusive lower bound of the amount drained.</summary>
        protected override int MinAmount => 30;

        /// <summary>Exclusive upper bound of the amount drained.</summary>
        protected override int MaxAmount => 140;

        /// <summary>Describes the indignity, given how much was actually taken.</summary>
        protected override string OnDrain(int amountLost)
        {
            return "A woman from your old high school messages 'hey hun!!' and by the next rest " +
                   "stop a pallet of berry-flavored 'metabolic gut activator' has replaced the " +
                   "actual food in the back -- you are a founding member of her downline now. " +
                   $"{amountLost} pounds of real snacks got traded for product nobody " +
                   "will ever buy, sample packets included.";
        }
    }
}
