// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Weather
{
    /// <summary>
    ///     An ice storm knocks out the grid and the heat with it, damaging supplies; this uses the item destroyer prefab
    ///     like the river crossings do.
    /// </summary>
    [DirectorEvent(EventCategory.Weather, EventExecution.ManualOnly)]
    public sealed class HailStorm : ItemDestroyer
    {
        /// <summary>Fired by the item destroyer event prefab before items are destroyed.</summary>
        /// <param name="destroyedItems"></param>
        /// <returns>The <see cref="string" />.</returns>
        protected override string OnPostDestroyItems(IDictionary<Entities, int> destroyedItems)
        {
            // Leggings are barter stock, not insulation, so cargo never decides who freezes. Every living passenger
            // takes the same roll. Before, two crates per person spared the party; the default four-crate,
            // four-person policy never reached that bar, so typical play already took this roll every time.
            return TryKillPassengers("frozen");
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

            // Lose 5-15 miles. Ported from the 1978 BASIC "TOTAL_MILES=TOTAL_MILES-5-RND(0)*10", where RND is a
            // 0-1 fraction; WolfCurses' Random.Next() is an integer in [0, 60), so the bounds are explicit.
            // Weather rolls from the location tick, before Vehicle.OnTick, so this comes off the carried mileage that
            // today's distance is built from (ahead of the pace multiplier and the random halving).
            vehicle?.ReduceMileage(GameSimulationApp.Instance.Random.Next(5, 16));
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
            floodPrompt.AppendLine("An ice storm takes down the grid. No lights,");
            floodPrompt.Append("no heat for days, resulting in");
            return floodPrompt.ToString();
        }
    }
}