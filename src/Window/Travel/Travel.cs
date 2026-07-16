// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Location;
using OregonTrailDotNet.Entity.Location.Point;
using OregonTrailDotNet.Event.Person;
using OregonTrailDotNet.Window.Travel.Command;
using OregonTrailDotNet.Window.Travel.Decision;
using OregonTrailDotNet.Window.Travel.Dialog;
using OregonTrailDotNet.Window.Travel.DoorDash.Help;
using OregonTrailDotNet.Window.Travel.Hunt.Help;
using OregonTrailDotNet.Window.Travel.Rest;
using OregonTrailDotNet.Window.Travel.RiverCrossing.Help;
using OregonTrailDotNet.Window.Travel.Store.Help;
using OregonTrailDotNet.Window.Travel.Trade;
using WolfCurses;
using WolfCurses.Window;

namespace OregonTrailDotNet.Window.Travel
{
    /// <summary>
    ///     Primary game Windows used for advancing simulation down the trail.
    /// </summary>
    public sealed class Travel : Window<TravelCommands, TravelInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Window{TCommands,TData}" /> class.
        /// </summary>
        /// <param name="simUnit">Core simulation which is controlling the form factory.</param>
        public Travel(SimulationApp simUnit) : base(simUnit)
        {
        }

        /// <summary>
        ///     Determines if the simulation should continue to check if the game has ended.
        /// </summary>
        private bool GameOver { get; set; }

        /// <summary>
        ///     One-shot guard so the Touchdown Jesus shrine death beat fires at most once per game.
        /// </summary>
        private bool _shrineBeatFired;

        /// <summary>
        ///     One-shot guards so each location-scripted forking decision fires at most once per game.
        /// </summary>
        private bool _buceesFired;

        private bool _caravanFired;

        private bool _armFired;

        private bool _checkpointFired;

        /// <summary>
        ///     Attaches state that picks strings from array at random to show from point of interest.
        /// </summary>
        internal void TalkToPeople()
        {
            SetForm(typeof(TalkToPeople.TalkToPeople));
        }

        /// <summary>
        ///     Attached store game Windows on top of existing game Windows for purchasing items from this location.
        /// </summary>
        internal void BuySupplies()
        {
            SetForm(typeof(Store.Store));
        }

        /// <summary>
        ///     Resumes the simulation and progression down the trail towards the next point of interest.
        /// </summary>
        internal void ContinueOnTrail()
        {
            // Check if player has already departed and we are just moving along again.
            if (GameSimulationApp.Instance.Trail.CurrentLocation.Status == LocationStatus.Departed)
            {
                SetForm(typeof(ContinueOnTrail));
                return;
            }

            // Depending on what kind of location we are heading towards we will invoke different forms.
            if (GameSimulationApp.Instance.Trail.CurrentLocation is Landmark ||
                GameSimulationApp.Instance.Trail.CurrentLocation is Settlement ||
                GameSimulationApp.Instance.Trail.CurrentLocation is TollRoad)
                SetForm(typeof(LocationDepart));
            else if (GameSimulationApp.Instance.Trail.CurrentLocation is Entity.Location.Point.RiverCrossing)
                SetForm(typeof(RiverCrossHelp));
            else if (GameSimulationApp.Instance.Trail.CurrentLocation is ForkInRoad)
                SetForm(typeof(LocationFork));
        }

        /// <summary>
        ///     Shows current load out for vehicle and player inventory items.
        /// </summary>
        internal void CheckSupplies()
        {
            SetForm(typeof(CheckSupplies));
        }

        /// <summary>
        ///     Shows players current position on the total trail along with progress indicators so they know how much more they
        ///     have and what they have accomplished.
        /// </summary>
        internal void LookAtMap()
        {
            SetForm(typeof(LookAtMap));
        }

        /// <summary>
        ///     Changes the number of miles the vehicle will attempt to move in a single day.
        /// </summary>
        internal void ChangePace()
        {
            SetForm(typeof(ChangePace));
        }

        /// <summary>
        ///     Changes the amount of food in pounds the vehicle party members will consume each day of the simulation.
        /// </summary>
        internal void ChangeFoodRations()
        {
            SetForm(typeof(ChangeRations));
        }

        /// <summary>
        ///     Attaches state that will ask how many days should be ticked while sitting still, if zero is entered then nothing
        ///     happens.
        /// </summary>
        internal void StopToRest()
        {
            SetForm(typeof(RestAmount));
        }

        /// <summary>
        ///     Looks through the traveling information data for any pending trades that people might want to make with you.
        /// </summary>
        internal void AttemptToTrade()
        {
            SetForm(typeof(Trading));
        }

        /// <summary>
        ///     Attaches a new Windows on top of this one that lets the player wade into the crowd and grab trays of food off
        ///     the tables for a specified time limit. Requires nothing but nerve; ammo is not involved in the food sweep.
        /// </summary>
        internal void HuntForFood()
        {
            SetForm(typeof(HuntingPrompt));
        }

        /// <summary>
        ///     Attaches the DoorDash gig prompt so the player can spend a day running deliveries in town for cash.
        /// </summary>
        internal void DriveForDoorDash()
        {
            SetForm(typeof(DoorDashPrompt));
        }

        /// <summary>
        ///     Header text shown above the base travel menu (distance/location status + "You may:").
        /// </summary>
        internal static string BuildMenuHeader()
        {
            var headerText = new StringBuilder();
            headerText.Append(TravelInfo.TravelStatus);
            headerText.AppendLine("You may:");
            return headerText.ToString();
        }

        /// <summary>
        ///     Determines if there is a store, people to get advice from, and a place to rest, what options are available at
        ///     this point of interest on the trail, and which handler each one invokes. Consumed by <see cref="TravelMenu" />
        ///     to build its arrow-navigable option list every render pass.
        /// </summary>
        internal IEnumerable<(TravelCommands Command, Action Handler)> GetMenuCommands()
        {
            var commands = new List<(TravelCommands, Action)>
            {
                (TravelCommands.ContinueOnTrail, ContinueOnTrail),
                (TravelCommands.CheckSupplies, CheckSupplies),
                (TravelCommands.LookAtMap, LookAtMap),
                (TravelCommands.ChangePace, ChangePace),
                (TravelCommands.ChangeFoodRations, ChangeFoodRations),
                (TravelCommands.StopToRest, StopToRest)
            };

            // Get a reference to current location on the trail so we can query it to build our menu.
            var location = GameSimulationApp.Instance.Trail.CurrentLocation;

            // Depending on where you are at on the trail the last few available commands change.
            switch (location.Status)
            {
                case LocationStatus.Unreached:
                    break;
                case LocationStatus.Arrived:
                    commands.Add((TravelCommands.AttemptToTrade, AttemptToTrade));

                    // Some commands are optional and change depending on location category.
                    if (location.ChattingAllowed)
                        commands.Add((TravelCommands.TalkToPeople, TalkToPeople));

                    if (location.ShoppingAllowed)
                    {
                        commands.Add((TravelCommands.BuySupplies, BuySupplies));

                        // DoorDash only operates in the towns big enough to shop in.
                        commands.Add((TravelCommands.DriveForDoorDash, DriveForDoorDash));
                    }
                    break;
                case LocationStatus.Departed:
                    commands.Add((TravelCommands.AttemptToTrade, AttemptToTrade));
                    commands.Add((TravelCommands.HuntForFood, HuntForFood));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return commands;
        }

        /// <summary>
        ///     Fired when the game Windows changes it's internal state. Allows the attached Windows to do special behaviors when
        ///     it realizes a state is set or removed and act on it. WolfCurses' own bare-window command menu (AddCommand) has
        ///     no rendering hook we can inject arrow-key highlighting into, so the base travel menu is instead always shown
        ///     through <see cref="TravelMenu" />, a normal Form. Whenever nothing else claims the form slot, re-attach it here.
        /// </summary>
        protected override void OnFormChange()
        {
            base.OnFormChange();

            if (CurrentForm == null)
                SetForm(typeof(TravelMenu));
        }

        /// <summary>
        ///     Called after the Windows has been added to list of modes and made active.
        /// </summary>
        public override void OnWindowPostCreate()
        {
            // Starting store that is shown after setting up player names, profession, and starting month.
            if (GameSimulationApp.Instance.Trail.IsFirstLocation &&
                (GameSimulationApp.Instance.Trail.CurrentLocation?.Status == LocationStatus.Unreached))
                SetForm(typeof(StoreWelcome));
            else
                SetForm(typeof(TravelMenu));
        }

        /// <summary>
        ///     Called when the Windows manager in simulation makes this Windows the currently active game Windows. Depending on
        ///     order of modes this might not get called until the Windows is actually ticked by the simulation.
        /// </summary>
        public override void OnWindowActivate()
        {
            ArriveAtLocation();
        }

        /// <summary>
        ///     On the first point we are going to force the look around state onto the traveling Windows without asking.
        /// </summary>
        private void ArriveAtLocation()
        {
            // Grab instance of game simulation for easy reading.
            var game = GameSimulationApp.Instance;

            // Skip ticking logic for travel mode if game is closing.
            if (game.IsClosing)
                return;

            // Skip if we have already ended the game.
            if (GameOver)
                return;

            // Check if passengers in the vehicle are dead, or player reached end of the trail.
            if (game.Trail.CurrentLocation.LastLocation || game.Vehicle.PassengersDead)
            {
                GameOver = true;
                game.WindowManager.Add(typeof(GameOver.GameOver));
                return;
            }

            // Check if player is just arriving at a new location.
            if ((game.Trail.CurrentLocation.Status == LocationStatus.Arrived) && !game.Trail.CurrentLocation.ArrivalFlag &&
                !GameOver)
            {
                game.Trail.CurrentLocation.ArrivalFlag = true;
                SetForm(typeof(LocationArrive));
                return;
            }

            // Design-spec §7/A3: the one location-scripted death beat. At the Touchdown Jesus shrine there is a
            // chance the 62-ft Styrofoam Jesus is struck by lightning and a party member is caught in the fire.
            // Only the location-gated trigger is new; the StyrofoamJesus event reuses the unchanged PersonInjure
            // mechanic. Guarded to fire at most once per game.
            if (!_shrineBeatFired &&
                game.Trail.CurrentLocation is Landmark &&
                game.Trail.CurrentLocation.Name.StartsWith("Touchdown Jesus") &&
                game.Vehicle.Passengers.Count > 0)
            {
                _shrineBeatFired = true;
                if (game.Random.Next(3) == 0)
                {
                    var victim = game.Vehicle.Passengers[game.Random.Next(game.Vehicle.Passengers.Count)];
                    game.EventDirector.TriggerEvent(victim, typeof(StyrofoamJesus));
                    return;
                }
            }

            // Location-scripted forking decisions. Each fires at most once per game and hands the player a numbered
            // choice Form; the chosen option is recorded in the ChoiceLedger for endgame scoring and epilogue recap.
            // NOTE: the "pack" decision is deliberately NOT triggered here -- it fires from LocationDepart as the
            // party pulls out of Cape Coral, so it cannot preempt the opening supply Store that OnWindowPostCreate
            // attaches while the first location is still Unreached.
            if (!_buceesFired && game.Trail.CurrentLocation.Name.StartsWith("Buc-ee's, Sevierville") &&
                game.Vehicle.Passengers.Count > 0)
            {
                _buceesFired = true;
                SetForm(typeof(BuceesHaulDecision));
                return;
            }

            if (!_caravanFired && game.Trail.CurrentLocation.Name.StartsWith("Carhenge") &&
                game.Vehicle.Passengers.Count > 0)
            {
                _caravanFired = true;
                SetForm(typeof(CaravanDecision));
                return;
            }

            if (!_armFired && game.Trail.CurrentLocation.Name.StartsWith("Open-Carry Walmart") &&
                game.Vehicle.Passengers.Count > 0)
            {
                _armFired = true;
                SetForm(typeof(ArmYourselfDecision));
                return;
            }

            if (!_checkpointFired && game.Trail.CurrentLocation.Name.StartsWith("Portland") &&
                game.Vehicle.Passengers.Count > 0)
            {
                _checkpointFired = true;
                SetForm(typeof(CheckpointDecision));
                return;
            }

            // Nothing else claimed the form slot this pass; show the base travel menu, but only if some other
            // form (e.g. StoreWelcome, set back in OnWindowPostCreate) isn't already active - this fallback
            // used to just refresh the command list, which was safe to call unconditionally, but SetForm is not.
            if (CurrentForm == null)
                SetForm(typeof(TravelMenu));
        }

        /// <summary>
        ///     Fired when the simulation adds a game Windows that is not this Windows. Used to execute code in other modes that
        ///     are not the active Windows anymore one last time.
        /// </summary>
        public override void OnWindowAdded()
        {
            // Skip if the player has already arrived at this location.
            if (!GameSimulationApp.Instance.Trail.CurrentLocation.ArrivalFlag)
                return;

            ArriveAtLocation();
        }
    }
}