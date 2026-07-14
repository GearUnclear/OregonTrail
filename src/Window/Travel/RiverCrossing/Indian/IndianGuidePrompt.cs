// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.UI;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.RiverCrossing.Indian
{
    /// <summary>
    ///     Prompts the player with a yes or no question regarding if they would like to use the services offered by the Indian
    ///     guide. However, he requires sets of clothing and not money like the ferry operator. If they player does not have
    ///     enough clothing in their inventory then the message will say so here since there is no opportunity to trade once
    ///     you are actually at the river crossing. The amount of clothing he asks for will also change based on the amount of
    ///     animals killed while hunting.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class IndianGuidePrompt : NumberedYesNoInputForm<TravelInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="IndianGuidePrompt" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public IndianGuidePrompt(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Changes up the behavior of the input dialog based on if the player has enough clothes to trade the Indian guide.
        /// </summary>
        protected override DialogType DialogType => HasEnoughClothingToTrade ? DialogType.YesNo : DialogType.Prompt;

        /// <summary>
        ///     Determines if the player has enough clothing to trade the Indian guide for his services in crossing the river.
        /// </summary>
        private bool HasEnoughClothingToTrade => GameSimulationApp.Instance.Vehicle.Inventory[Entities.Clothes].Quantity >=
                                                 UserData.River.IndianCost;

        /// <summary>
        ///     Only allows input from the player if they have enough clothing to trade with the Indian guide, otherwise we will
        ///     treat this as a prompt only and no input.
        /// </summary>
        public override bool InputFillsBuffer => HasEnoughClothingToTrade;

        /// <summary>
        ///     Tracks the arrow-key highlighted line between the Yes/No options when the trade can be made.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Cached result of <see cref="OnDialogPrompt" />, computed once (mirroring the base InputForm's own
        ///     call-once-then-cache contract) rather than every render tick.
        /// </summary>
        private string _promptText;

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
        ///     Returns a text only representation of the current game Windows state. When the player has enough clothing
        ///     to trade this is a real Yes/No choice and gets an arrow-navigable menu; otherwise it's a plain message.
        /// </summary>
        public override string OnRenderForm()
        {
            var text = _promptText;

            if (!HasEnoughClothingToTrade)
                return text;

            var menu = new List<ArrowMenuOption>
            {
                new ArrowMenuOption("1. Yes", "y"),
                new ArrowMenuOption("2. No", "n")
            };
            _menu.SetOptions(menu);
            GameSimulationApp.Instance.ActiveMenu = _menu;
            return text + Environment.NewLine + _menu.Render();
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            // Builds up the first part about the sovereign-citizen local for the flooded crossing.
            var prompt = new StringBuilder();
            prompt.AppendLine($"{Environment.NewLine}A local says he isn't driving,");
            prompt.AppendLine("he's traveling, and will guide");
            prompt.AppendLine($"your car across for {UserData.River.IndianCost:N0}");
            prompt.AppendLine($"crates of MLM leggings.{Environment.NewLine}");

            // Change up the message based on if the player has enough leggings, they won't be able to get more if they don't here.
            if (HasEnoughClothingToTrade)
            {
                // Player has enough leggings to satisfy the local's cost.
                prompt.AppendLine("Will you accept this");
                prompt.Append("offer? Y/N");
            }
            else
            {
                // Player does not have enough leggings to satisfy the local's cost.
                prompt.AppendLine($"You don't have {UserData.River.IndianCost:N0} crates of");
                prompt.AppendLine($"leggings.{Environment.NewLine}");
            }

            // Renders out the Indian guide river crossing confirmation and or denial.
            return prompt.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // Depending on if the player has enough clothing their response to Indian guide changes.
            if (HasEnoughClothingToTrade)
                switch (reponse)
                {
                    case DialogResponse.Yes:
                        UserData.River.CrossingType = RiverCrossChoice.Indian;
                        SetForm(typeof(UseIndianConfirm));
                        break;
                    case DialogResponse.No:
                    case DialogResponse.Custom:
                        CancelIndianCrossing();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(reponse), reponse, null);
                }
            else
                CancelIndianCrossing();
        }

        /// <summary>
        ///     Player does not have enough clothing to satisfy the Indian cost.
        /// </summary>
        private void CancelIndianCrossing()
        {
            UserData.River.CrossingType = RiverCrossChoice.None;
            SetForm(typeof(RiverCross));
        }
    }
}