// The Asphalt Trail (2028 re-skin) -- SUPPLY_DRAIN (cash).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: turning a parking space into a trap and a citizen into a revenue event. Grounded in the well-
    ///     documented predatory-towing economy -- cars hooked minutes after a lot's rules change, "drop fees" charged
    ///     for releasing a vehicle still on the hook, cash-only impound yards with weekend hours nobody can make.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class ThePredatoryTow : ModernSupplyDrain
    {
        /// <summary>Which inventory category this drain empties.</summary>
        protected override Entities DrainCategory => Entities.Cash;

        /// <summary>Inclusive lower bound of the amount drained.</summary>
        protected override int MinAmount => 80;

        /// <summary>Exclusive upper bound of the amount drained.</summary>
        protected override int MaxAmount => 280;

        /// <summary>Describes the indignity, given how much was actually taken.</summary>
        protected override string OnDrain(int amountLost)
        {
            return "You are inside the diner for eleven minutes. Sometime this week the\n" +
                   "lot changed hands and grew new signs, and your car is already on the\n" +
                   "hook when you walk out. The drop fee is cash only, the yard closes in\n" +
                   $"four minutes, and the fee is ${amountLost}. The number on the sign\n" +
                   "goes to a voicemail box that is full.";
        }
    }
}
