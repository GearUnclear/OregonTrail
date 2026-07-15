// The Asphalt Trail (2028 re-skin) -- scripted flavor event (ManualOnly).

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Modern
{
    /// <summary>
    ///     Allegory: the pre-order -- paying full price, up front, for a thing that does not exist yet, because the
    ///     marketing got to everyone in the car at once. Red Dead Redemption 3 drops mid-trip and every living
    ///     passenger has, independently and without discussion, pre-ordered the $80 edition. Charged clean off the
    ///     party's cash.
    ///
    ///     ManualOnly: this never appears in the random ModernHazard pool. It is fired by a dedicated 0.02%/turn
    ///     roll in ContinueOnTrail (Random.Next(5000) == 0), per spec.
    /// </summary>
    [DirectorEvent(EventCategory.ModernHazard, EventExecution.ManualOnly)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class RedDeadRedemption3 : EventProduct
    {
        /// <summary>Per-passenger pre-order price, in dollars.</summary>
        private const int PreOrderPerPerson = 80;

        /// <summary>Cached outcome text so the render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>Acts as the constructor; the factory allocates events uninitialized and calls this instead.</summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();
            _outcome = "Red Dead Redemption 3 comes out.";
        }

        /// <summary>Charges every living passenger's pre-order to the party's cash and narrates it.</summary>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            if (vehicle == null)
            {
                _outcome = "Red Dead Redemption 3 comes out, but there is no one to charge.";
                return;
            }

            // One pre-order per living passenger, at $80 each.
            var buyers = vehicle.Passengers.Count(p => p.HealthStatus != HealthStatus.Dead);
            var total = buyers * PreOrderPerPerson;
            vehicle.Inventory[Entities.Cash].ReduceQuantity(total);

            var story = new StringBuilder();
            story.AppendLine("Red Dead Redemption 3 comes out.");
            story.AppendLine();
            story.AppendLine($"Everyone in the car pre-ordered it. All {buyers} of them, independently,");
            story.AppendLine("without a word to each other, at the $80 Ultimate tier with the horse-armor");
            story.AppendLine("cosmetic and the three-day early access nobody will use because you are, at");
            story.AppendLine("present, driving.");
            story.AppendLine();
            story.Append($"${total} leaves the party account. The map is enormous, they hear.");

            _outcome = story.ToString();
        }

        /// <summary>Text user interface string explaining the charge.</summary>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }
    }
}
