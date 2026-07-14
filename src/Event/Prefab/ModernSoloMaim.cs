// The Asphalt Trail (2028 re-skin) -- modern-hazard prefab.

using System.Linq;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Prefab
{
    /// <summary>
    ///     One-off "SOLO_MAIM" profile: a single random passenger is badly hurt and left injured, and the convoy
    ///     loses road speed while dealing with it. The heavy damage roll can occasionally be fatal on its own and,
    ///     because the victim is now flagged injured, they are more likely to spiral on later health ticks.
    ///     Mirrored by SOLO_MAIM in sim/Program.cs (weight ~27%): Damage(60,200) + Injure + ReduceMileage(15).
    ///     Subclasses phrase the maiming via <see cref="OnMaim" />.
    /// </summary>
    public abstract class ModernSoloMaim : EventProduct
    {
        /// <summary>Cached outcome text so the render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>Acts as the constructor; the factory allocates events uninitialized and calls this instead.</summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();
            _outcome = "One of your travelers is badly hurt.";
        }

        /// <summary>Picks a random living passenger, badly injures them, slows the convoy, and narrates it.</summary>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            var game = GameSimulationApp.Instance;

            var living = vehicle?.Passengers.Where(p => p.HealthStatus != HealthStatus.Dead).ToList();
            if (living == null || living.Count <= 0)
            {
                _outcome = "One of your travelers is badly hurt.";
                return;
            }

            var victim = living[game.Random.Next(living.Count)];
            _outcome = OnMaim(victim);

            victim.Damage(game.Random.Next(60, 200));
            victim.Injure();
            vehicle.ReduceMileage(15);
        }

        /// <summary>Text user interface string explaining the injury.</summary>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }

        /// <summary>Describes how this particular passenger was maimed.</summary>
        /// <param name="victim">The passenger who is about to be badly hurt.</param>
        protected abstract string OnMaim(Entity.Person.Person victim);
    }
}
