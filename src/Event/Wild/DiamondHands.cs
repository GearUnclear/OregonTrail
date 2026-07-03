// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A whole crowd is dumping its savings into a dying mall store to stick it to the hedge funds. Diamond hands. It
    ///     is a coin flip: the party either rides it to the moon or gets wiped out.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class DiamondHands : EventProduct
    {
        /// <summary>
        ///     Holds the outcome text for the coin flip so it can be shown to the player on render.
        /// </summary>
        private string _outcome;

        /// <summary>
        ///     Fired when the event is created by the event factory, but before it is executed. Acts as a constructor mostly but
        ///     used in this way so that only the factory will call the method.
        /// </summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();

            // Default outcome so render is never empty even before execution.
            _outcome = "You put it all on the dying mall store.";
        }

        /// <summary>
        ///     Fired when the event handler associated with this enum type triggers action on target entity. Implementation is
        ///     left completely up to handler.
        /// </summary>
        /// <param name="eventExecutor">
        ///     Entities which the event is going to directly affect. This way there is no confusion about
        ///     what entity the event is for. Will require casting to correct instance type from interface instance.
        /// </param>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            // Cast the source entity as vehicle.
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;

            // Skip if the source entity is not a vehicle.
            if (vehicle == null)
                return;

            // Coin flip: moon or wipeout. Same payoff size either direction.
            if (GameSimulationApp.Instance.Random.NextBool())
            {
                vehicle.Inventory[Entities.Food].AddQuantity(14);
                _outcome = "To the moon. The position paid off and the larder is full.";
            }
            else
            {
                vehicle.Inventory[Entities.Food].ReduceQuantity(14);
                _outcome = "Wiped out. The store closed and the savings went with it.";
            }
        }

        /// <summary>
        ///     Fired when the simulation would like to render the event, typically this is done AFTER executing it but this could
        ///     change depending on requirements of the implementation.
        /// </summary>
        /// <param name="userData"></param>
        /// <returns>Text user interface string that can be used to explain what the event did when executed.</returns>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }
    }
}
