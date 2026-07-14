// The Asphalt Trail (2028 re-skin) -- modern-hazard prefab.

using System.Linq;
using System.Text;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Prefab
{
    /// <summary>
    ///     Whole-party "PILEUP" profile: a genuinely fatal multi-car catastrophe. Every caught passenger has a
    ///     real (35%) chance of dying outright; the rest are badly hurt. It can wipe even a healthy convoy (rarely)
    ///     and reliably finishes a thinned one. Mirrored by PILEUP in sim/Program.cs (weight ~18%): per living
    ///     passenger, 35% kill else Damage(50,170). Subclasses supply the flavor; the toll numbers are appended
    ///     automatically so the render reflects what actually happened.
    /// </summary>
    public abstract class ModernPileup : EventProduct
    {
        /// <summary>Cached outcome text so the render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>Acts as the constructor; the factory allocates events uninitialized and calls this instead.</summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();
            _outcome = "The convoy is caught in a catastrophe on the road.";
        }

        /// <summary>Rolls each living passenger for death or heavy injury, then narrates the toll.</summary>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            var game = GameSimulationApp.Instance;

            var story = new StringBuilder();
            story.AppendLine(OnPileup());

            var killed = 0;
            var hurt = 0;
            if (vehicle != null)
                foreach (var passenger in vehicle.Passengers.ToList())
                {
                    if (passenger.HealthStatus == HealthStatus.Dead)
                        continue;

                    // 35% killed outright; otherwise heavy blunt damage that may still prove fatal.
                    if (game.Random.Next(100) < 35)
                    {
                        passenger.Kill();
                        killed++;
                    }
                    else
                    {
                        passenger.Damage(game.Random.Next(50, 170));
                        if (passenger.HealthStatus == HealthStatus.Dead)
                            killed++;
                        else
                            hurt++;
                    }
                }

            story.AppendLine();
            if (killed > 0 && hurt > 0)
                story.Append($"{killed} did not make it; {hurt} more were badly hurt.");
            else if (killed > 0)
                story.Append(killed == 1 ? "One traveler did not make it." : $"{killed} travelers did not make it.");
            else if (hurt > 0)
                story.Append(hurt == 1 ? "One traveler was badly hurt." : $"{hurt} travelers were badly hurt.");
            else
                story.Append("The convoy walks away shaken but whole.");

            _outcome = story.ToString();
        }

        /// <summary>Text user interface string explaining the pileup.</summary>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }

        /// <summary>The flavor line describing the specific multi-car catastrophe.</summary>
        protected abstract string OnPileup();
    }
}
