// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.UI;
using OregonTrailDotNet.Window.Travel.Command;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.Dialog
{
    /// <summary>
    ///     Asks the player if they would like to stop and check out a tombstone that is on this particular mile marker.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class TombstoneQuestion : InputForm<TravelInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TombstoneQuestion" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public TombstoneQuestion(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Tracks the arrow-key highlighted line between the Yes/No options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Cached result of <see cref="OnDialogPrompt" />, computed once (mirroring the base InputForm's own
        ///     call-once-then-cache contract) rather than every render tick.
        /// </summary>
        private string _promptText;

        /// <summary>
        ///     Defines what type of dialog this will act like depending on this enumeration value. Up to implementation to define
        ///     desired behavior.
        /// </summary>
        protected override DialogType DialogType => DialogType.YesNo;

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
            var pointReached = new StringBuilder();

            // Build up message about there being something on the side of the road.
            pointReached.AppendLine(
                $"{Environment.NewLine}You pass a roadside memorial. Would you");
            pointReached.Append("like to look closer? Y/N");

            return pointReached.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // Check if the player wants to look at the tombstone or not.
            switch (reponse)
            {
                case DialogResponse.No:
                    SetForm(typeof(ContinueOnTrail));
                    break;
                case DialogResponse.Yes:
                case DialogResponse.Custom:
                    GameSimulationApp.Instance.WindowManager.Add(typeof(Graveyard.Graveyard));

                    // Goes back to continue on trail form below us.
                    ClearForm();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reponse), reponse, null);
            }
        }
    }
}