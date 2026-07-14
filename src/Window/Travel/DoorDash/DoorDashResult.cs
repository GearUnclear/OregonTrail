// Created for the 2028 Asphalt Trail re-skin -- DoorDash gig mini-game.

using System;
using System.Text;
using OregonTrailDotNet.Entity.Item;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.DoorDash
{
    /// <summary>
    ///     Tallies a finished DoorDash shift and shows the player the honest net: gross pay minus the fuel and tire life they
    ///     spent to earn it, valued at real replacement prices. The whole shift also cost a full day. More often than not the
    ///     number at the bottom makes the player wonder why they bothered -- which is the point.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class DoorDashResult : InputForm<TravelInfo>
    {
        private readonly StringBuilder _shiftScore;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DoorDashResult" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public DoorDashResult(IWindow window) : base(window)
        {
            _shiftScore = new StringBuilder();
        }

        /// <summary>
        ///     Fired after the state attaches; the shift consumed a full day so we take a turn (party eats, health rolls).
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();
            GameSimulationApp.Instance.TakeTurn(false);
        }

        /// <summary>Builds the itemized shift reckoning.</summary>
        /// <returns>The result text.</returns>
        protected override string OnDialogPrompt()
        {
            var dash = UserData.DoorDash;
            _shiftScore.Clear();

            // Value what you actually spent at real replacement prices: fuel at the local pump, tires at $200 a piece.
            var fuelCost = dash.CansBurned * FuelPricing.CurrentCost();
            var wearCost = dash.TiresWorn * Parts.Wheel.Cost;
            var net = dash.GrossEarned - fuelCost - wearCost;

            _shiftScore.AppendLine($"{Environment.NewLine}You clock out after {dash.DeliveriesDone} deliveries.");
            _shiftScore.AppendLine($"{Environment.NewLine}Gross (base + tips): {dash.GrossEarned:C2}");

            if (dash.CancelledOnYou > 0)
                _shiftScore.AppendLine($"  ({dash.CancelledOnYou} order(s) cancelled on you after pickup)");

            _shiftScore.AppendLine($"Fuel burned: {dash.CansBurned} can(s)  -{fuelCost:C2}");
            _shiftScore.AppendLine($"Tires worn:  {dash.TiresWorn} tire(s) -{wearCost:C2}");

            if (dash.DroveOnWornTires)
                _shiftScore.AppendLine("  (out of spares -- you finished on bald tires)");

            _shiftScore.AppendLine($"{Environment.NewLine}Net for the day: {net:C2}");
            _shiftScore.AppendLine(net < 0
                ? $"You lost {Math.Abs(net):C2} and a whole day. Was it worth it?"
                : $"You cleared {net:C2} for a full day of driving. Was it worth it?");

            return _shiftScore.ToString();
        }

        /// <summary>Tears down the shift and returns to the travel menu.</summary>
        /// <param name="reponse">The dialog response.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            UserData.DestroyDoorDash();
            ClearForm();
        }
    }
}
