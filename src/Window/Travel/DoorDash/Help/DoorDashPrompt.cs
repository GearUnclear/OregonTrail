// Created for the 2028 Asphalt Trail re-skin — DoorDash gig mini-game.

using System;
using System.Text;
using OregonTrailDotNet.Entity;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.DoorDash.Help
{
    /// <summary>
    ///     Prompt that precedes a DoorDash shift when accessed from the travel menu. Explains — honestly — how the gig works
    ///     before the player commits a day to it. Refuses to start the shift if the SUV has no fuel to drive on.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class DoorDashPrompt : InputForm<TravelInfo>
    {
        private readonly StringBuilder _dashHelp;

        /// <summary>Whether the player actually has fuel to run a shift; set when the prompt is built.</summary>
        private bool _canDash;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DoorDashPrompt" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public DoorDashPrompt(IWindow window) : base(window)
        {
            _dashHelp = new StringBuilder();
        }

        /// <summary>Builds the explanation (or the "no fuel" refusal).</summary>
        /// <returns>The prompt text.</returns>
        protected override string OnDialogPrompt()
        {
            _dashHelp.Clear();

            _canDash = GameSimulationApp.Instance.Vehicle.Inventory[Entities.Animal].Quantity > 0;

            _dashHelp.AppendLine($"{Environment.NewLine}Drive for DoorDash{Environment.NewLine}");

            if (!_canDash)
            {
                _dashHelp.AppendLine(
                    ("You have no gas cans. You can't run a delivery shift without fuel to drive on. " +
                     "Fill up at the travel center first.").WordWrap());
                return _dashHelp.ToString();
            }

            const string dashTop =
                "A shift takes the whole day. Offers roll in one at a time; type ACCEPT to take one or REJECT to skip it. " +
                "Base pay is a couple of dollars — the money is supposed to be in the tips, but the tip you see is only an " +
                "estimate and customers can (and do) yank it back after delivery.";

            const string dashBottom =
                "Every mile you drive — including the unpaid miles out to the restaurant — burns your own gas and wears out " +
                "your tires. Reject too many offers and the app starts feeding you worse ones. Type QUIT any time to clock out.";

            _dashHelp.AppendLine(dashTop.WordWrap());
            _dashHelp.AppendLine(dashBottom.WordWrap());

            return _dashHelp.ToString();
        }

        /// <summary>Starts the shift, or bows out if there was no fuel.</summary>
        /// <param name="reponse">The dialog response.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            if (!_canDash)
            {
                ClearForm();
                return;
            }

            UserData.GenerateDoorDash();
            SetForm(typeof(DoorDash));
        }
    }
}
