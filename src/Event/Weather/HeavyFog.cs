// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Weather
{
    /// <summary>
    ///     Reduces the total capacity for the vehicle to move in a given trip segment by a random amount calculated at the
    ///     time of event execution.
    /// </summary>
    [DirectorEvent(EventCategory.Weather)]
    public sealed class HeavyFog : EventProduct
    {
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

            // Lose 10-15 miles. Ported from the 1978 BASIC "TOTAL_MILES=TOTAL_MILES-10-5*RND(0)", where RND is a
            // 0-1 fraction; WolfCurses' Random.Next() is an integer in [0, 60), so the bounds are explicit.
            // Weather rolls from the location tick, before Vehicle.OnTick, so this comes off the carried mileage that
            // today's distance is built from (ahead of the pace multiplier and the random halving).
            vehicle?.ReduceMileage(GameSimulationApp.Instance.Random.Next(10, 16));
        }

        /// <summary>
        ///     Fired when the simulation would like to render the event, typically this is done AFTER executing it but this could
        ///     change depending on requirements of the implementation.
        /// </summary>
        /// <param name="userData">
        ///     Entities which the event is going to directly affect. This way there is no confusion about
        ///     what entity the event is for. Will require casting to correct instance type from interface instance.
        /// </param>
        /// <returns>Text user interface string that can be used to explain what the event did when executed.</returns>
        protected override string OnRender(RandomEventInfo userData)
        {
            return "You lose your way in AQI-484 wildfire smoke under an orange sky---time is lost.";
        }
    }
}