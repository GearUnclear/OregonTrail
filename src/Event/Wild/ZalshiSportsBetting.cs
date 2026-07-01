// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A guy at the charging station named Zalshi swears he is printing money on the Zalshi Sportsbook app and sends
    ///     the party a referral. The on-paper balance climbs all evening and it feels like free money, right up until the
    ///     cash-out button demands a "verification deposit," locks the account, and hands you a chatbot. The deposit,
    ///     $350, is simply gone.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class ZalshiSportsBetting : EventProduct
    {
        /// <summary>The "verification deposit" the party can never claw back once the account locks.</summary>
        private const int LockedDeposit = 350;

        /// <summary>Holds the outcome text so render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>
        ///     Fired when the event is created by the event factory, but before it is executed. Acts as a constructor mostly but
        ///     used in this way so that only the factory will call the method.
        /// </summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();

            _outcome = "A guy named Zalshi shows you his sportsbook winnings and sends a referral.";
        }

        /// <summary>
        ///     Fired when the event handler associated with this enum type triggers action on target entity.
        /// </summary>
        /// <param name="eventExecutor">Entities which the event is going to directly affect.</param>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            // Cast the source entity as vehicle; bail if it is not one so we never touch a null inventory.
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            if (vehicle == null)
                return;

            // The money is gone the moment the account locks. Reduce first, narrate the con second.
            vehicle.Inventory[Entities.Cash].ReduceQuantity(LockedDeposit);

            var story = new StringBuilder();
            story.AppendLine("At a charging station a guy named Zalshi flashes his phone: up $4,000");
            story.AppendLine("this month on the Zalshi Sportsbook. \"It's free money, here's my referral.\"");
            story.AppendLine("You buy in and your balance climbs to $900 by nightfall. Easy.");
            story.AppendLine();
            story.AppendLine("Then you hit CASH OUT. The app wants a \"verification deposit,\" then locks");
            story.Append($"the account. Support is a chatbot. Your ${LockedDeposit} is gone.");

            _outcome = story.ToString();
        }

        /// <summary>
        ///     Fired when the simulation would like to render the event, typically this is done AFTER executing it.
        /// </summary>
        /// <param name="userData">Source entity information for the event.</param>
        /// <returns>Text user interface string explaining what the scam did.</returns>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }
    }
}
