// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.GameOver
{
    /// <summary>
    ///     Fired when the simulation has determined the entire party has died (a total wipe). Rather than dumping the player
    ///     straight to the epitaph flow, we first show a proper end-of-run summary: who did not make it, how far the SUV got,
    ///     and a lead-in to the final Net Worth and Clout tally. The player must acknowledge this screen before the run is
    ///     scored (via <see cref="FinalPoints" />) and the GoFundMe epitaph headstone is offered.
    /// </summary>
    [ParentWindow(typeof(GameOver))]
    public sealed class GameFail : InputForm<GameOverInfo>
    {
        /// <summary>
        ///     Holds reference to the bleak end-of-run recap shown to the player.
        /// </summary>
        private readonly StringBuilder _fail;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameFail" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public GameFail(IWindow window) : base(window)
        {
            _fail = new StringBuilder();
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The dialog prompt text.<see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            var game = GameSimulationApp.Instance;
            var party = game.Vehicle.Passengers;
            var count = party.Count;

            _fail.AppendLine($"{Environment.NewLine}THE ROAD TRIP IS OVER{Environment.NewLine}");
            _fail.AppendLine("Sunshine State Mutual has closed your policy.");
            _fail.AppendLine("Nobody reached Seattle. Your GoFundMe has been");
            _fail.AppendLine($"quietly marked \"campaign ended.\"{Environment.NewLine}");

            // Name everyone who did not make it -- a total wipe means the whole manifest is gone.
            if (count == 4)
                _fail.AppendLine("All four travelers are gone:");
            else if (count == 1)
                _fail.AppendLine("Your lone traveler is gone:");
            else
                _fail.AppendLine($"The entire party of {count} is gone:");

            foreach (var person in party)
                _fail.AppendLine($"  - {person.Name}");

            // Final distance the SUV managed before the party ran out of luck.
            _fail.AppendLine(
                $"{Environment.NewLine}Final odometer: {game.Vehicle.Odometer:N0} miles of asphalt.{Environment.NewLine}");
            _fail.AppendLine("Let's tally what the estate is worth to the algorithm.");

            return _fail.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // Score the doomed run and show the Net Worth and Clout tally before the epitaph/main menu.
            SetForm(typeof(FinalPoints));
        }
    }
}
