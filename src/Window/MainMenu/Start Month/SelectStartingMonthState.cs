// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OregonTrailDotNet.Module.Time;
using OregonTrailDotNet.UI;
using OregonTrailDotNet.Window.MainMenu.Help;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.MainMenu.Start_Month
{
    /// <summary>
    ///     Offers the player the ability to change the starting month of the simulation, this affects how many resources will
    ///     be available to them and the severity of the random events they encounter along the trail.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class SelectStartingMonthState : Form<NewGameInfo>
    {
        /// <summary>
        ///     References the string representing the question about starting month, only builds it once and holds in memory while
        ///     state is active.
        /// </summary>
        private StringBuilder _startMonthQuestion;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the starting month choices.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="SelectStartingMonthState" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public SelectStartingMonthState(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>Default is FALSE. Setting to TRUE allows characters and input buffer to be read when submitted.</remarks>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            // Pass the game data to the simulation for each new game Windows state.
            GameSimulationApp.Instance.SetStartInfo(UserData);

            // Tell the user they need to make a decision.
            _startMonthQuestion = new StringBuilder();
            _startMonthQuestion.AppendLine($"{Environment.NewLine}The home insurance lapsed. Your");
            _startMonthQuestion.AppendLine("jumping off place for Seattle is");
            _startMonthQuestion.AppendLine("Cape Coral, Florida. You must");
            _startMonthQuestion.Append("decide which month to leave");
        }

        /// <summary>
        ///     Returns a text only representation of the current game Windows state. Could be a statement, information, question
        ///     waiting input, etc.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string OnRenderForm()
        {
            // Rebuilt every render pass so the arrow-key highlight stays current.
            var choices = new List<StartingMonth>(Enum.GetValues(typeof(StartingMonth)).Cast<StartingMonth>());

            var options = new List<ArrowMenuOption>();
            foreach (var monthValue in choices)
            {
                options.Add(new ArrowMenuOption($"{(int) monthValue}. {monthValue}", ((int) monthValue).ToString()));
            }

            options.Add(new ArrowMenuOption($"{choices.Count + 1}. Ask for advice", (choices.Count + 1).ToString()));

            _menu.SetOptions(options);
            GameSimulationApp.Instance.ActiveMenu = _menu;

            return _startMonthQuestion + Environment.NewLine + _menu.Render();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Skip if the input is null or empty.
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
                return;

            // Attempt to cast string to enum value, can be characters or integer.
            Enum.TryParse(input, out StartingMonth startMonth);

            // Depending on what was selected we will set starting month to correct one in full listing, or show advice to player.
            switch (startMonth)
            {
                case StartingMonth.March:
                    UserData.StartingMonth = Month.March;
                    SetForm(typeof(InitialItemsHelp));
                    break;
                case StartingMonth.April:
                    UserData.StartingMonth = Month.April;
                    SetForm(typeof(InitialItemsHelp));
                    break;
                case StartingMonth.May:
                    UserData.StartingMonth = Month.May;
                    SetForm(typeof(InitialItemsHelp));
                    break;
                case StartingMonth.June:
                    UserData.StartingMonth = Month.June;
                    SetForm(typeof(InitialItemsHelp));
                    break;
                case StartingMonth.July:
                    UserData.StartingMonth = Month.July;
                    SetForm(typeof(InitialItemsHelp));
                    break;
                default:


// Shows information about what the different starting months mean.
                    SetForm(typeof(StartMonthHelp));
                    break;
            }
        }
    }
}