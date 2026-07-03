// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.MainMenu.Start_Month
{
    /// <summary>
    ///     Shows the player information about what the various starting months mean.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class StartMonthHelp : InputForm<NewGameInfo>
    {
        /// <summary>Initializes a new instance of the <see cref="StartMonthHelp" /> class.</summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public StartMonthHelp(IWindow window) : base(window)
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
            // Inform the user about a decision they need to make.
            var startMonthHelp = new StringBuilder();
            startMonthHelp.Append($"{Environment.NewLine}You sit through a gas-station{Environment.NewLine}");
            startMonthHelp.Append($"meeting for \"folks with the{Environment.NewLine}");
            startMonthHelp.Append($"climate-refugee itch.\" You're told:{Environment.NewLine}{Environment.NewLine}");
            startMonthHelp.Append($"If you leave too early, the heat{Environment.NewLine}");
            startMonthHelp.Append($"domes haven't broken and gas is{Environment.NewLine}");
            startMonthHelp.Append($"scarce. If you leave too late,{Environment.NewLine}");
            startMonthHelp.Append($"you may not reach Seattle before{Environment.NewLine}");
            startMonthHelp.Append($"the mountain passes ice over. If{Environment.NewLine}");
            startMonthHelp.Append($"you leave at just the right time,{Environment.NewLine}");
            startMonthHelp.Append($"the roads are clear and the{Environment.NewLine}");
            startMonthHelp.Append($"weather will still be cool.{Environment.NewLine}{Environment.NewLine}");
            return startMonthHelp.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // parentGameMode.State = new SelectStartingMonthState(parentGameMode, UserData);
            SetForm(typeof(SelectStartingMonthState));
        }
    }
}