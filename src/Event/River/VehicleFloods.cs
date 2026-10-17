// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.River
{
    /// <summary>
    ///     When taking the washed-out shoulder detour there is a chance that your SUV will flood out if you choose to seal
    ///     the doors and float across the high water.
    /// </summary>
    [DirectorEvent(EventCategory.RiverCross, EventExecution.ManualOnly)]
    public sealed class VehicleFloods : ItemDestroyer
    {
        /// <summary>Fired by the item destroyer event prefab before items are destroyed.</summary>
        /// <param name="destroyedItems">Items that were destroyed from the players inventory.</param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostDestroyItems(IDictionary<Entities, int> destroyedItems)
        {
            return destroyedItems.Count > 0
                ? TryKillPassengers("drowned")
                : "no loss of items.";
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
            base.Execute(eventExecutor);

            // Cast the source entity as vehicle.
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;

            // Lose 20-40 miles. Ported from the 1978 BASIC "TOTAL_MILES=TOTAL_MILES-20-20*RND(0)", where RND is a
            // 0-1 fraction; WolfCurses' Random.Next() is an integer in [0, 60), so the bounds are explicit.
            // NOTE: this is a no-op today. The only trigger is CrossingTick, which parks the vehicle first, and
            // ReduceMileage ignores a parked vehicle; a crossing costs supplies, drownings, and crossing days instead.
            // The draw is kept so the random stream (and the pinned crossing seeds) does not shift.
            vehicle?.ReduceMileage(GameSimulationApp.Instance.Random.Next(20, 41));
        }

        /// <summary>
        ///     Fired by the item destroyer event prefab after items are destroyed.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnPreDestroyItems()
        {
            var floodPrompt = new StringBuilder();
            floodPrompt.Clear();
            floodPrompt.AppendLine("The car floods out");
            floodPrompt.AppendLine("on the washed-out shoulder");
            floodPrompt.Append("detour, resulting in");
            return floodPrompt.ToString();
        }
    }
}