// The Asphalt Trail (2028 re-skin) -- modern-hazard prefab.

using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Prefab
{
    /// <summary>
    ///     "SUPPLY_DRAIN" profile: a non-lethal modern indignity that drains a supply category instead of a life.
    ///     A cash subclass extracts money (a scam, an impound, a legal settlement); a food subclass spoils or
    ///     converts snacks, pushing the party toward a slow starvation loss. Mirrored by SUPPLY_DRAIN in
    ///     sim/Program.cs (weight ~24%, roughly 55% food / 45% cash). Subclasses pick the category and range via
    ///     <see cref="DrainCategory" /> / <see cref="MinAmount" /> / <see cref="MaxAmount" /> and phrase the loss.
    /// </summary>
    public abstract class ModernSupplyDrain : EventProduct
    {
        /// <summary>Cached outcome text so the render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>Which inventory category this drain empties (Cash or Food).</summary>
        protected abstract Entities DrainCategory { get; }

        /// <summary>Inclusive lower bound of the amount drained.</summary>
        protected abstract int MinAmount { get; }

        /// <summary>Exclusive upper bound of the amount drained (System.Random.Next semantics).</summary>
        protected abstract int MaxAmount { get; }

        /// <summary>Acts as the constructor; the factory allocates events uninitialized and calls this instead.</summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();
            _outcome = "The road costs you something.";
        }

        /// <summary>Drains the chosen category by a random amount (clamped by the item's floor) and narrates it.</summary>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            if (vehicle == null)
            {
                _outcome = "The road costs you something.";
                return;
            }

            var game = GameSimulationApp.Instance;
            var item = vehicle.Inventory[DrainCategory];
            var before = item.Quantity;
            var want = game.Random.Next(MinAmount, MaxAmount);
            item.ReduceQuantity(want);
            var lost = before - item.Quantity;

            _outcome = OnDrain(lost);
        }

        /// <summary>Text user interface string explaining what was taken.</summary>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }

        /// <summary>Describes the indignity, given how much was actually taken.</summary>
        /// <param name="amountLost">The amount actually removed (may be less than requested near the floor).</param>
        protected abstract string OnDrain(int amountLost);
    }
}
