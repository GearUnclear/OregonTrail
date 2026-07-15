// Created for the 2028 Asphalt Trail re-skin -- DoorDash gig mini-game.

using System;
using System.Text;
using OregonTrailDotNet.Entity;
using WolfCurses;

namespace OregonTrailDotNet.Window.Travel.DoorDash
{
    /// <summary>
    ///     Runs a single DoorDash shift: surfaces delivery offers the player accepts or rejects, tracks the cash earned, and --
    ///     critically -- burns the player's own gas cans and spare tires to do it. Rejecting offers is not free either: the
    ///     acceptance-rate algorithm quietly hands a picky driver worse and worse offers. The whole thing is designed to feel
    ///     productive while usually being a wash once fuel and wear are honestly subtracted (see <see cref="DoorDashResult" />).
    /// </summary>
    public sealed class DoorDashManager : ITick
    {
        /// <summary>Total number of shift ticks in a single day of Dashing.</summary>
        public const int SHIFTTIME = 30;

        /// <summary>Total miles (paid + deadhead) that burn one five-gallon gas can in city stop-and-go.</summary>
        private const int MILESPERCAN = 30;

        /// <summary>Total miles that wear out one spare tire.</summary>
        private const int WEARPERTIRE = 45;

        /// <summary>Worst-case offer quality when acceptance rate has cratered.</summary>
        private const double ACCEPTRATE_FLOOR = 0.4;

        /// <summary>Chance an accepted order is cancelled after pickup -- you drove for it but collect only base pay.</summary>
        private const double CANCEL_CHANCE = 0.08;

        private DeliveryOffer _currentOffer;
        private int _secondsRemaining;

        // Rolling accumulators: miles bank up until they cost a whole can / tire.
        private int _milesDriven;
        private int _wearMiles;

        // Shift tallies used by the result screen.
        private double _grossEarned;
        private int _deliveriesDone;
        private int _offersSeen;
        private int _accepted;
        private int _cansBurned;
        private int _tiresWorn;
        private int _cancelledOnYou;
        private bool _droveOnWornTires;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DoorDashManager" /> class and starts the shift clock.
        /// </summary>
        public DoorDashManager()
        {
            _secondsRemaining = SHIFTTIME;
        }

        /// <summary>Total dollars (base + realized tips) collected this shift, before fuel/wear costs.</summary>
        public double GrossEarned => _grossEarned;

        /// <summary>Number of completed deliveries.</summary>
        public int DeliveriesDone => _deliveriesDone;

        /// <summary>Gas cans consumed this shift.</summary>
        public int CansBurned => _cansBurned;

        /// <summary>Spare tires worn out this shift.</summary>
        public int TiresWorn => _tiresWorn;

        /// <summary>Orders that were cancelled after pickup, costing you the tip.</summary>
        public int CancelledOnYou => _cancelledOnYou;

        /// <summary>TRUE if the SUV kept Dashing after its last spare tire was gone.</summary>
        public bool DroveOnWornTires => _droveOnWornTires;

        /// <summary>Whether there is currently an offer awaiting an accept/reject decision.</summary>
        public bool HasOffer => _currentOffer != null;

        /// <summary>Acceptance rate so far; starts optimistic at 100% so the first offers are decent.</summary>
        private double AcceptanceRate => _offersSeen <= 0 ? 1.0 : _accepted / (double) _offersSeen;

        /// <summary>Quality of the next offer the algorithm will surface, driven by acceptance rate.</summary>
        private double OfferQuality => ACCEPTRATE_FLOOR + (1.0 - ACCEPTRATE_FLOOR) * AcceptanceRate;

        /// <summary>Current gas cans remaining in the vehicle.</summary>
        private static int GasRemaining => GameSimulationApp.Instance.Vehicle.Inventory[Entities.Animal].Quantity;

        /// <summary>Shift is over when the clock runs out or the SUV is out of fuel.</summary>
        public bool ShouldEndShift => (_secondsRemaining <= 0) || (GasRemaining <= 0);

        /// <summary>Renders the live shift status the player reads while deciding on offers.</summary>
        public string ShiftInfo
        {
            get
            {
                var game = GameSimulationApp.Instance;
                var status = new StringBuilder();

                status.AppendLine($"{Environment.NewLine}--------------------------------");
                status.AppendLine($"Dashing near {game.Trail.CurrentLocation?.Name}");

                var shiftPercentage = _secondsRemaining / (decimal) SHIFTTIME;
                status.AppendLine($"Shift left: {shiftPercentage * 100:N0}%");
                status.AppendLine($"Earned so far: {_grossEarned:C2} ({_deliveriesDone} drops)");
                status.AppendLine($"Fuel: {GasRemaining} gas cans");
                status.AppendLine($"Spare tires: {game.Vehicle.Inventory[Entities.Wheel].Quantity}");
                status.AppendLine($"Acceptance rate: {AcceptanceRate * 100:N0}%");
                status.AppendLine("--------------------------------");

                if (_currentOffer != null)
                {
                    status.AppendLine($"{Environment.NewLine}NEW OFFER");
                    status.AppendLine($"{_currentOffer.Restaurant} -> {_currentOffer.Dropoff}");
                    status.AppendLine($"Base pay: {_currentOffer.BasePay:C2}");
                    status.AppendLine($"Est. tip: {_currentOffer.EstimatedTip:C2} (estimated)");
                    status.AppendLine($"Distance: {_currentOffer.PaidDistance} mi");

                    var circling = _currentOffer.DecidePercentage;
                    status.AppendLine($"Another Dasher circling: {circling * 100:N0}%");
                    status.AppendLine($"{Environment.NewLine}Type ACCEPT to take it, REJECT to skip.");
                }
                else
                {
                    status.AppendLine($"{Environment.NewLine}Waiting for an offer to come in...");
                }

                status.Append($"{Environment.NewLine}(type QUIT to clock out)");
                return status.ToString();
            }
        }

        /// <summary>
        ///     Ticks the shift: burns the clock, ages the current offer (withdrawing it if the player dithered), and otherwise
        ///     tries to surface a new offer.
        /// </summary>
        /// <param name="systemTick">TRUE if this is an unpredictable OS tick, which we ignore.</param>
        /// <param name="skipDay">TRUE if time was force-ticked without a day passing.</param>
        public void OnTick(bool systemTick, bool skipDay)
        {
            if (systemTick || skipDay)
                return;

            if (_secondsRemaining <= 0)
                return;

            _secondsRemaining--;

            if (_currentOffer != null)
            {
                _currentOffer.TickDecide();
                if (_currentOffer.Expired)
                    _currentOffer = null;

                return;
            }

            TrySurfaceOffer();
        }

        /// <summary>Surfaces a fresh offer (not every tick) scaled by the current acceptance-rate quality.</summary>
        private void TrySurfaceOffer()
        {
            if (_currentOffer != null)
                return;

            // Offers do not arrive every single tick.
            if (GameSimulationApp.Instance.Random.NextBool())
                return;

            _currentOffer = new DeliveryOffer(OfferQuality);
            _offersSeen++;
        }

        /// <summary>
        ///     Accepts the current offer: drives it (burning fuel and tires on the total miles, losing shift time to the wait),
        ///     then collects base pay plus whatever tip actually materializes -- unless the order gets cancelled on you.
        /// </summary>
        public void Accept()
        {
            var offer = _currentOffer;
            if (offer == null)
                return;

            var game = GameSimulationApp.Instance;

            // Every mile -- paid or unpaid deadhead -- costs you fuel and tire life; the wait eats your clock.
            _milesDriven += offer.TotalMiles;
            _wearMiles += offer.TotalMiles;
            _secondsRemaining -= offer.WaitTicks;
            if (_secondsRemaining < 0)
                _secondsRemaining = 0;

            BurnFuel();
            WearTires();

            // Collect the money. Cash is stored as dollars in the inventory item, mirroring how Hunting banks food.
            double pay;
            if (game.Random.NextDouble() < CANCEL_CHANCE)
            {
                pay = offer.BasePay;
                _cancelledOnYou++;
            }
            else
            {
                pay = offer.BasePay + offer.RealizedTip();
            }

            _grossEarned += pay;
            game.Vehicle.Inventory[Entities.Cash].AddQuantity((int) Math.Round(pay));

            _deliveriesDone++;
            _accepted++;
            _currentOffer = null;
            game.InputManager.ClearBuffer();
        }

        /// <summary>Rejects the current offer. No money, and the acceptance rate quietly drops for next time.</summary>
        public void Reject()
        {
            _currentOffer = null;
            GameSimulationApp.Instance.InputManager.ClearBuffer();
        }

        /// <summary>Ends the shift immediately (the player chose to clock out).</summary>
        public void ClockOut()
        {
            _secondsRemaining = 0;
        }

        /// <summary>Converts banked miles into consumed gas cans.</summary>
        private void BurnFuel()
        {
            var gas = GameSimulationApp.Instance.Vehicle.Inventory[Entities.Animal];
            while (_milesDriven >= MILESPERCAN)
            {
                _milesDriven -= MILESPERCAN;
                if (gas.Quantity <= 0)
                    break;

                gas.ReduceQuantity(1);
                _cansBurned++;
            }
        }

        /// <summary>Converts banked miles into worn-out spare tires; notes when the SUV runs out of spares.</summary>
        private void WearTires()
        {
            var tires = GameSimulationApp.Instance.Vehicle.Inventory[Entities.Wheel];
            while (_wearMiles >= WEARPERTIRE)
            {
                _wearMiles -= WEARPERTIRE;
                if (tires.Quantity <= 0)
                {
                    _droveOnWornTires = true;
                    break;
                }

                tires.ReduceQuantity(1);
                _tiresWorn++;
            }
        }
    }
}
