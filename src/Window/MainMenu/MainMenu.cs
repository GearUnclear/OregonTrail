// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Renderer;
using OregonTrailDotNet.Window.MainMenu.Help;
using OregonTrailDotNet.Window.MainMenu.Options;
using OregonTrailDotNet.Window.MainMenu.Profession;
using WolfCurses;
using WolfCurses.Window;

namespace OregonTrailDotNet.Window.MainMenu
{
    /// <summary>
    ///     Allows the configuration of party names, player profession, and purchasing initial items for trip.
    /// </summary>
    public sealed class MainMenu : Window<MainMenuCommands, NewGameInfo>
    {
        /// <summary>
        ///     Asked for the first party member.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public const string LEADER_QUESTION = "What is the first name of the driver?";

        /// <summary>
        ///     Asked for every other party member name we want to collect. Interpolates the chosen vehicle's seating
        ///     capacity so the question always matches how many other names are actually being asked for.
        /// </summary>
        public static string MembersQuestion =>
            $"What are the first names of the{Environment.NewLine}" +
            $"{GameSimulationApp.Instance.Vehicle.MaxPartySize - 1} other members of your family?";

        /// <summary>
        ///     Initializes a new instance of the <see cref="Window{TCommands,TData}" /> class.
        /// </summary>
        /// <param name="simUnit">Core simulation which is controlling the form factory.</param>
        // ReSharper disable once UnusedMember.Global
        public MainMenu(SimulationApp simUnit) : base(simUnit)
        {
        }

        /// <summary>
        ///     Called after the Windows has been added to list of modes and made active. WolfCurses' bare-window
        ///     AddCommand menu has no rendering hook we can highlight, so the menu is shown through
        ///     <see cref="MainMenuScreen" />, a normal Form, instead.
        /// </summary>
        public override void OnWindowPostCreate()
        {
            SetForm(typeof(MainMenuScreen));
        }

        /// <summary>
        ///     Header text shown above the main menu (title art + "You may:").
        /// </summary>
        internal static string BuildMenuHeader()
        {
            var headerText = new StringBuilder();
            headerText.AppendLine(SceneArt.Title);
            headerText.AppendLine($"{Environment.NewLine}You may:");
            return headerText.ToString();
        }

        /// <summary>
        ///     The main menu's commands and the handler each one invokes, in display order. Consumed by
        ///     <see cref="MainMenuScreen" /> to build its arrow-navigable option list every render pass.
        /// </summary>
        internal IEnumerable<(MainMenuCommands Command, Action Handler)> GetMenuCommands()
        {
            return new List<(MainMenuCommands, Action)>
            {
                (MainMenuCommands.TravelTheTrail, TravelTheTrail),
                (MainMenuCommands.LearnAboutTheTrail, LearnAboutTrail),
                (MainMenuCommands.SeeTheOregonTopTen, SeeTopTen),
                (MainMenuCommands.ChooseManagementOptions, ChooseManagementOptions),
                (MainMenuCommands.CloseSimulation, CloseSimulation)
            };
        }

        /// <summary>
        ///     Fired when the game Windows changes its internal state; whenever nothing else claims the form slot,
        ///     re-attach the main menu (mirrors the equivalent guard in <c>Travel.OnFormChange</c>).
        /// </summary>
        protected override void OnFormChange()
        {
            base.OnFormChange();

            if (CurrentForm == null)
                SetForm(typeof(MainMenuScreen));
        }

        /// <summary>
        ///     Does exactly what it says on the tin, closes the simulation and releases all memory.
        /// </summary>
        private static void CloseSimulation()
        {
            GameSimulationApp.Instance.Destroy();
        }

        /// <summary>
        ///     Glorified options menu, used to clear top ten, Tombstone messages, and saved games.
        /// </summary>
        private void ChooseManagementOptions()
        {
            SetForm(typeof(ManagementOptions));
        }

        /// <summary>
        ///     High score list, defaults to hard-coded values if no custom ones present.
        /// </summary>
        private void SeeTopTen()
        {
            SetForm(typeof(CurrentTopTen));
        }

        /// <summary>
        ///     Instruction manual that explains how the game works and what is expected of the player.
        /// </summary>
        private void LearnAboutTrail()
        {
            SetForm(typeof(RulesHelp));
        }

        /// <summary>
        ///     Start the new-game flow with the satirical preamble that sets up the 2028 roadtrip; once the player has
        ///     read it, <see cref="GameIntro" /> hands off to profession selection and the rest of the chain.
        /// </summary>
        private void TravelTheTrail()
        {
            SetForm(typeof(GameIntro));
        }
    }
}