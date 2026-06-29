// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.MainMenu.Help
{
    /// <summary>
    ///     Shows basic information about how the game works, how traveling works, rules for winning and losing.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class RulesHelp : InputForm<NewGameInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RulesHelp" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public RulesHelp(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            var aboutTrail = new StringBuilder();
            aboutTrail.AppendLine($"{Environment.NewLine}Your drive up the Asphalt Trail begins in Cape Coral, Flor");
            aboutTrail.AppendLine("ida. With the place uninsurable, you plan to take your family");
            aboutTrail.AppendLine(
                $"over {GameSimulationApp.Instance.Trail.Length:N0} tough miles to Seattle, Washington.{Environment.NewLine}");

            aboutTrail.AppendLine("Having paid off the SUV, you now");
            aboutTrail.AppendLine($"have to purchase the following supplies:{Environment.NewLine}");

            aboutTrail.AppendLine(
                " * Cans of gas (spending more will buy a fuller load which lets");
            aboutTrail.AppendLine($" you cover more ground so you're on the road for less time){Environment.NewLine}");

            aboutTrail.AppendLine(
                $" * Snacks (you'll need ample snacks to keep up your strength and health){Environment.NewLine}");

            aboutTrail.AppendLine(" * Ammo (sold by the flour, no permit, license, or check. You'll");
            aboutTrail.AppendLine($" need ammo for the food sweep and for fighting off forfeiture stops){Environment.NewLine}");

            aboutTrail.AppendLine(" * Leggings (crates of MLM leggings, the only barter the sovereign-");
            aboutTrail.AppendLine($" citizen river guide will take when the Interstate's underwater){Environment.NewLine}");

            aboutTrail.AppendLine(" * Other supplies (includes first-aid, tools, and spare tires,");
            aboutTrail.AppendLine($" alternators, and transmissions for breakdowns){Environment.NewLine}");
            return aboutTrail.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // parentGameMode.State = null;
            ClearForm();
        }
    }
}