// The Asphalt Trail (2028 re-skin) -- modern-hazard prefab.

using System.Linq;
using System.Text;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Prefab
{
    /// <summary>
    ///     One-off "SOLO_KILL" profile: a single random living passenger dies in a signature satirical death.
    ///     It does not end the run by itself (the game is lost only when the WHOLE party is dead), but attrition
    ///     thins the convoy so the next pileup can wipe it. Mirrored by SOLO_KILL in sim/Program.cs (weight ~24%).
    ///     Subclasses phrase the death via <see cref="OnDeath" />, which receives the chosen victim.
    /// </summary>
    public abstract class ModernSoloDeath : EventProduct
    {
        /// <summary>Cached outcome text so the render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>Acts as the constructor; the factory allocates events uninitialized and calls this instead.</summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();
            _outcome = "The road takes one of your travelers.";
        }

        /// <summary>Picks a random living passenger, kills them, and narrates it.</summary>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            var game = GameSimulationApp.Instance;

            var living = vehicle?.Passengers.Where(p => p.HealthStatus != HealthStatus.Dead).ToList();
            if (living == null || living.Count <= 0)
            {
                _outcome = "The road takes one of your travelers.";
                return;
            }

            var victim = living[game.Random.Next(living.Count)];
            var story = new StringBuilder();
            story.Append(OnDeath(victim));
            victim.Kill();

            _outcome = story.ToString();
        }

        /// <summary>Text user interface string explaining the death.</summary>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }

        /// <summary>Describes how this particular passenger met their end.</summary>
        /// <param name="victim">The passenger who is about to die.</param>
        protected abstract string OnDeath(Entity.Person.Person victim);
    }
}
