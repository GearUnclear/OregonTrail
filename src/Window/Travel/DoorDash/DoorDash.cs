// Created for the 2028 Asphalt Trail re-skin -- DoorDash gig mini-game.

using System;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.DoorDash
{
    /// <summary>
    ///     Interactive DoorDash shift screen. Offers scroll in over the course of the shift; the player types ACCEPT or REJECT
    ///     to decide on each one, or QUIT to clock out early. Accepting burns the party's own fuel and tires, so the "reward"
    ///     always carries a real cost. When the shift clock runs out (or the SUV runs dry) the result screen tallies the honest
    ///     net.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class DoorDash : Form<TravelInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DoorDash" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public DoorDash(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        public override bool InputFillsBuffer => !UserData.DoorDash.ShouldEndShift;

        /// <summary>
        ///     Determines if this state is allowed to receive any input at all.
        /// </summary>
        public override bool AllowInput => !UserData.DoorDash.ShouldEndShift;

        /// <summary>
        ///     Called when the simulation is ticked; advances the shift and hands off to the result screen when it is over.
        /// </summary>
        /// <param name="systemTick">TRUE if ticked unpredictably by the OS.</param>
        /// <param name="skipDay">TRUE if the simulation force-ticked without advancing a day.</param>
        public override void OnTick(bool systemTick, bool skipDay)
        {
            base.OnTick(systemTick, skipDay);

            if (UserData.DoorDash.ShouldEndShift)
                SetForm(typeof(DoorDashResult));
            else
                UserData.DoorDash?.OnTick(systemTick, skipDay);
        }

        /// <summary>
        ///     Returns the live shift status for rendering.
        /// </summary>
        /// <returns>The text user interface.</returns>
        public override string OnRenderForm()
        {
            return UserData.DoorDash.ShiftInfo;
        }

        /// <summary>Fired when the player submits input while the shift is running.</summary>
        /// <param name="input">Contents of the input buffer.</param>
        public override void OnInputBufferReturned(string input)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
                return;

            var command = input.Trim();

            // Clocking out is always allowed, even with no offer on screen.
            if (command.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("done", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                UserData.DoorDash.ClockOut();
                SetForm(typeof(DoorDashResult));
                return;
            }

            // Accept/reject only mean something when there is an offer waiting.
            if (!UserData.DoorDash.HasOffer)
                return;

            if (command.Equals("accept", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("a", StringComparison.OrdinalIgnoreCase))
            {
                UserData.DoorDash.Accept();
                return;
            }

            if (command.Equals("reject", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("r", StringComparison.OrdinalIgnoreCase))
                UserData.DoorDash.Reject();
        }
    }
}
