// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Entity;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    /// <summary>
    ///     Tabulates results about the food sweep after it ends, depending on the performance of the player and how many
    ///     trays they grabbed, if any will be calculated in weight. Players can only ever haul 100 pounds of food back to
    ///     the vehicle so this discourages grabbing more than they can carry.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class HuntingResult : InputForm<TravelInfo>
    {
        /// <summary>
        ///     References the total weight of all the animals the player killed so we only have to reference it once.
        /// </summary>
        private int _finalKillWeight;

        /// <summary>
        ///     Holds all of the data for our final hunting result before rendering out to player.
        /// </summary>
        private readonly StringBuilder _huntScore;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InputForm{T}" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public HuntingResult(IWindow window) : base(window)
        {
            _huntScore = new StringBuilder();
        }

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            // After hunting we roll the dice on the party and player and skip a day.
            GameSimulationApp.Instance.TakeTurn(false);
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The dialog prompt text.<see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            // Clear previous hunting score information.
            _huntScore.Clear();

            // Calculate total weight of all trays grabbed by player during the food sweep.
            var killWeight = UserData.Hunt.KillWeight;

            // Depending on grabbed weight we change response and message.
            if (killWeight <= 0)
            {
                _huntScore.AppendLine($"{Environment.NewLine}You came away empty-handed with no");
                _huntScore.AppendLine($"food.{Environment.NewLine}");
            }
            else if (killWeight > 0)
            {
                // Message to let the player know they grabbed some trays.
                _huntScore.AppendLine($"{Environment.NewLine}From the trays you swept up, you");
                _huntScore.AppendLine($"got {killWeight:N0} pounds of food.{Environment.NewLine}");

                // Adds the grabbed weight since it is safe at this point.
                _finalKillWeight = killWeight;

                // Player can only take MAXFOOD amount from sweep regardless of total weight.
                if (killWeight <= HuntManager.MAXFOOD)
                    return _huntScore.ToString();

                // Forces the weight of the haul to become
                _finalKillWeight = HuntManager.MAXFOOD;

                // Player grabbed too many trays.
                _huntScore.AppendLine("However, you were only able to");
                _huntScore.AppendLine($"carry {_finalKillWeight:N0} pounds back to the");
                _huntScore.AppendLine($"car.{Environment.NewLine}");
            }

            // Return the hunting result to text renderer.
            return _huntScore.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // Transfers the total finalized kill weight we calculated to vehicle inventory as food in pounds.
            if (_finalKillWeight > 0)
                GameSimulationApp.Instance.Vehicle.Inventory[Entities.Food].AddQuantity(_finalKillWeight);

            // Destroys all hunting related data now that we are done with it.
            UserData.DestroyHunt();

            // Returns to the travel menu so the player can continue down the trail.
            ClearForm();
        }
    }
}