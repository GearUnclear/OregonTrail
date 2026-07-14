// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.UI;
using OregonTrailDotNet.Window.MainMenu.Start_Month;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.MainMenu.Names
{
    /// <summary>
    ///     Prints out every entered player name in the user data for simulation initialization. Confirms with the player they
    ///     would indeed like to use all the entered names they have provided or had randomly generated for them by just
    ///     pressing enter.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class ConfirmPlayerNames : NumberedYesNoInputForm<NewGameInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConfirmPlayerNames" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public ConfirmPlayerNames(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Tracks the arrow-key highlighted line between the Yes/No options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Cached result of <see cref="OnDialogPrompt" />, computed once (mirroring the base InputForm's own
        ///     call-once-then-cache contract) rather than every render tick. This one matters more than most:
        ///     OnDialogPrompt() below calls GameSimulationApp.Instance.SetStartInfo(UserData), which appends a
        ///     Person to the vehicle for every party name - calling it every render tick instead of once would
        ///     have flooded the vehicle's passenger list with duplicates for as long as this screen stayed on screen.
        /// </summary>
        private string _promptText;

        /// <summary>
        ///     Defines what type of dialog this will act like depending on this enumeration value. Up to implementation to define
        ///     desired behavior.
        /// </summary>
        protected override DialogType DialogType => DialogType.Custom;

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();
            _promptText = OnDialogPrompt();
        }

        /// <summary>
        ///     Returns a text only representation of the current game Windows state, with an arrow-navigable Yes/No menu
        ///     appended below the dialog prompt.
        /// </summary>
        public override string OnRenderForm()
        {
            // This screen renders a numbered "1. Yes / 2. No" menu, so the numbers a player types MUST
            // work. The base InputForm's DialogType.Custom parser only understands the letter/word forms
            // (Y/YES/TRUE and N/NO/FALSE) -- typing "1" used to fall through to the Custom branch, which
            // quietly wiped the whole party back to the first-name prompt. The shared
            // NumberedYesNoInputForm base now remaps "1"/"2" to those letters so digit, letter, and
            // arrow-select all agree.
            var menu = new List<ArrowMenuOption>
            {
                new ArrowMenuOption("1. Yes", "y"),
                new ArrowMenuOption("2. No", "n")
            };
            _menu.SetOptions(menu);
            GameSimulationApp.Instance.ActiveMenu = _menu;
            return _promptText + Environment.NewLine + _menu.Render();
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            // Pass the game data to the simulation for each new game Windows state.
            GameSimulationApp.Instance.SetStartInfo(UserData);

            // Create string builder, counter, print info about party members.
            var confirmPartyText = new StringBuilder();
            confirmPartyText.AppendLine(
                $"{Environment.NewLine}Are these names correct? Y/N{Environment.NewLine}");
            var crewNumber = 1;

            // Loop through every player and print their name.
            for (var index = 0; index < UserData.PlayerNames.Count; index++)
            {
                // First name in list is always the leader.
                var name = UserData.PlayerNames[index];
                var isLeader = (UserData.PlayerNames.IndexOf(name) == 0) && (crewNumber == 1);

                // Only append new line when not printing last line.
                if (index < UserData.PlayerNames.Count - 1)
                    confirmPartyText.AppendLine(isLeader
                        ? $"  {crewNumber} - {name} (leader)"
                        : $"  {crewNumber} - {name}");
                else
                    confirmPartyText.Append($"  {crewNumber} - {name}");

                crewNumber++;
            }

            return confirmPartyText.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            switch (reponse)
            {
                case DialogResponse.No:
                    RestartNameInput();
                    break;
                case DialogResponse.Yes:
                    UserData.PlayerNameIndex = 0;
                    SetForm(typeof(SelectStartingMonthState));
                    break;
                case DialogResponse.Custom:
                    RestartNameInput();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reponse), reponse, null);
            }
        }

        /// <summary>
        ///     Restarts the player name selection.
        /// </summary>
        private void RestartNameInput()
        {
            UserData.PlayerNames.Clear();
            UserData.PlayerNameIndex = 0;
            SetForm(typeof(InputPlayerNames));
        }
    }
}