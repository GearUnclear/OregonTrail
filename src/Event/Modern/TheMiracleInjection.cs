// The Asphalt Trail (2028 re-skin) -- SUPPLY_DRAIN (food).

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: engineering hunger out of the human being and calling it wellness. Grounded in the GLP-1
    ///     (Ozempic/Wegovy) boom, gray-market compounded and peptide knockoffs bought online, and the resulting
    ///     appetite collapse -- a cooler of snacks nobody in the car can stand to look at anymore.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TheMiracleInjection : ModernSupplyDrain
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
            return "Half the convoy is on the gray-market weight-loss pens now, ordered from a compounding pharmacy an " +
                   "influencer swore by, and appetite has left the vehicle entirely. The gas-station hauls get bought " +
                   $"out of habit and thrown out untouched. {amountLost} pounds of snacks spoil in the cooler and go in " +
                   "a dumpster outside Amarillo.";
        }
    }
}
