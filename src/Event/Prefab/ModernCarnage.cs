// The Asphalt Trail (2028 re-skin) -- modern-hazard prefab.

using System.Linq;
using System.Text;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Prefab
{
    /// <summary>
    ///     Whole-party "CARNAGE" profile: a total-loss catastrophe that kills EVERYONE in the car at once
    ///     (a door-lock fire, off the gorge overlook, a wrong-way interstate collision). It is the sharp, rare
    ///     fatal edge of the modern-hazard table -- an instant, guaranteed party wipe that routes cleanly into the
    ///     GameFail screen via <see cref="Entity.Vehicle.Vehicle.PassengersDead" />. Subclasses supply only the
    ///     flavor line; the numeric effect is fixed and mirrored by CARNAGE in sim/Program.cs (weight ~7%).
    /// </summary>
    public abstract class ModernCarnage : EventProduct
    {
        /// <summary>Cached outcome text so the render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>Acts as the constructor; the factory allocates events uninitialized and calls this instead.</summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();
            _outcome = "A catastrophe overtakes the convoy.";
        }

        /// <summary>Kills every living passenger and narrates the wipe.</summary>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;

            var story = new StringBuilder();
            story.AppendLine(OnCarnage());

            if (vehicle != null)
                foreach (var passenger in vehicle.Passengers.ToList())
                    if (passenger.HealthStatus != HealthStatus.Dead)
                        passenger.Kill();

            story.AppendLine();
            story.Append("There are no survivors.");
            _outcome = story.ToString();
        }

        /// <summary>Text user interface string explaining the catastrophe.</summary>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }

        /// <summary>The flavor line describing the specific catastrophe that wiped the convoy.</summary>
        protected abstract string OnCarnage();
    }
}
