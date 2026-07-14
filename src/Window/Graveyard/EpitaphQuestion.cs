// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.UI;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Graveyard
{
    /// <summary>
    ///     Asks the user if they would like to write a custom message on their Tombstone for other users to see when the
    ///     come across this part of the trail in the future.
    /// </summary>
    [ParentWindow(typeof(Graveyard))]
    public sealed class EpitaphQuestion : NumberedYesNoInputForm<TombstoneInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EpitaphQuestion" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public EpitaphQuestion(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Tracks the arrow-key highlighted line between the Yes/No options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Cached result of <see cref="OnDialogPrompt" />, computed once (mirroring the base InputForm's own
        ///     call-once-then-cache contract) rather than every render tick - some OnDialogPrompt overrides in this
        ///     codebase have side effects that must not repeat on every tick.
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
            var epitaphPrompt = new StringBuilder();

            // Add Tombstone message with here lies player name, no epitaph yet.
            epitaphPrompt.Clear();
            epitaphPrompt.Append($"{Environment.NewLine}{UserData.Tombstone}");
            epitaphPrompt.AppendLine($"{Environment.NewLine}Would you like to write a");
            epitaphPrompt.Append("GoFundMe epitaph? Y/N");
            return epitaphPrompt.ToString();
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
                case DialogResponse.Yes:
                    SetForm(typeof(EpitaphEditor));
                    break;
                case DialogResponse.No:
                case DialogResponse.Custom:
                    GameSimulationApp.Instance.Tombstone.Add(UserData.Tombstone);
                    SetForm(typeof(TombstoneView));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reponse), reponse, null);
            }
        }
    }
}