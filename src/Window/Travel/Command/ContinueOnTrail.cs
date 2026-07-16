// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Entity.Location;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Event;
using OregonTrailDotNet.Event.Modern;
using OregonTrailDotNet.Renderer;
using OregonTrailDotNet.Window.Travel.Dialog;
using WolfCurses.Window;
using WolfCurses.Window.Control;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Command
{
    /// <summary>
    ///     Attached to the travel Windows when the player requests to continue on the trail. This shows a ping-pong progress
    ///     bar moving back and fourth which lets the player know they are moving. Stats are also shown from the travel info
    ///     object, if any random events occur they will be selected from this state.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class ContinueOnTrail : Form<TravelInfo>
    {
        /// <summary>
        ///     Holds the current drive state, since we can size up the situation at any time.
        /// </summary>
        private StringBuilder _drive;

        /// <summary>
        ///     Animated sway bar that prints out as text, ping-pongs back and fourth between left and right side, moved by
        ///     stepping it with tick.
        /// </summary>
        private MarqueeBar _marqueeBar;

        /// <summary>
        ///     Holds the text related to animated sway bar, each tick of simulation steps it.
        /// </summary>
        private string _swayBarText;

        /// <summary>
        ///     Ever-incrementing step for the scrolling roadside "driving" scene; advanced each simulation tick so
        ///     the world parallax-scrolls past the stationary SUV.
        /// </summary>
        private int _animStep;

        /// <summary>
        ///     Simulation ticks elapsed since the last calendar day was consumed. See <see cref="TicksPerDay" />.
        /// </summary>
        private int _ticksSinceDay;

        /// <summary>
        ///     How many simulation ticks (WolfCurses fires one per second) a single day of driving lasts.
        ///     At 1:1 the whole trail scrolled by in well under a minute and the roadside scene was on screen
        ///     for barely a second between random events, so the driving -- the only place the vehicle art is
        ///     ever shown -- read as a loading screen between dialogs. Stretching a day over several ticks
        ///     costs nothing in balance (the day is what the simulation counts, not the tick) and simply lets
        ///     the scene animate and be looked at. Raise to slow the drive down further, lower to speed it up.
        /// </summary>
        private const int TicksPerDay = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContinueOnTrail" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public ContinueOnTrail(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>Default is FALSE. Setting to TRUE allows characters and input buffer to be read when submitted.</remarks>
        public override bool InputFillsBuffer => false;

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            // Get instance of game simulation for easy reading.
            var game = GameSimulationApp.Instance;

            // We don't create it in the constructor, will update with ticks.
            _drive = new StringBuilder();

            // Animated sway bar.
            _marqueeBar = new MarqueeBar();
            _swayBarText = _marqueeBar.Step();

            // Vehicle has departed the current location for the next one but you can only depart once.
            if ((game.Trail.DistanceToNextLocation > 0) &&
                (game.Trail.CurrentLocation.Status == LocationStatus.Arrived))
                game.Trail.CurrentLocation.Status = LocationStatus.Departed;
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
            // Clear whatever was in the string builder last tick.
            _drive.Clear();

            // Animated roadside scene: the SUV holds still while the world scrolls past it, showing movement.
            _drive.AppendLine($"{Environment.NewLine}{SceneArt.TravelScene(_animStep)}");

            // Basic information about simulation.
            _drive.AppendLine(TravelInfo.DriveStatus);

            // Don't add the RETURN KEY text here if we are not actually at a point.
            _drive.Append("Press ENTER to size up the situation");

            // Wait for user input, event, or reaching next location...
            return _drive.ToString();
        }

        /// <summary>
        ///     Called when the simulation is ticked by underlying operating system, game engine, or potato. Each of these system
        ///     ticks is called at unpredictable rates, however if not a system tick that means the simulation has processed enough
        ///     of them to fire off event for fixed interval that is set in the core simulation by constant in milliseconds.
        /// </summary>
        /// <remarks>Default is one second or 1000ms.</remarks>
        /// <param name="systemTick">
        ///     TRUE if ticked unpredictably by underlying operating system, game engine, or potato. FALSE if
        ///     pulsed by game simulation at fixed interval.
        /// </param>
        /// <param name="skipDay">
        ///     Determines if the simulation has force ticked without advancing time or down the trail. Used by
        ///     special events that want to simulate passage of time without actually any actual time moving by.
        /// </param>
        public override void OnTick(bool systemTick, bool skipDay)
        {
            base.OnTick(systemTick, skipDay);

            // Only game simulation ticks please.
            if (systemTick)
                return;

            // Get instance of game simulation for easy reading.
            var game = GameSimulationApp.Instance;

            // Checks if the vehicle is stuck or broken, if not it is set to moving state.
            game.Vehicle.CheckStatus();

            // Determine if we should continue down the trail based on current vehicle status.
            switch (game.Vehicle.Status)
            {
                case VehicleStatus.Stopped:
                    return;
                case VehicleStatus.Disabled:
                    // Check if vehicle was able to obtain spare parts for repairs.
                    SetForm(typeof(UnableToContinue));
                    break;
                case VehicleStatus.Moving:
                    // Step the roadside scene on every tick so the world keeps scrolling past the vehicle even
                    // on the ticks that don't consume a day -- this is what makes the drive read as movement.
                    _swayBarText = _marqueeBar.Step();
                    _animStep++;

                    // Check if there is a tombstone here, if so we attach question form that asks if we stop or not.
                    // Cheap and idempotent: the odometer only moves on a day tick, so testing it every tick is fine.
                    if (game.Tombstone.ContainsTombstone(game.Vehicle.Odometer) &&
                        !game.Trail.CurrentLocation.ArrivalFlag)
                    {
                        SetForm(typeof(TombstoneQuestion));
                        return;
                    }

                    // Hold the day (and therefore every event roll below) for TicksPerDay ticks so the scene
                    // above stays on screen. Everything past this point happens once per in-game day, exactly
                    // as before -- only the wall-clock spacing between days changes.
                    if (++_ticksSinceDay < TicksPerDay)
                        return;
                    _ticksSinceDay = 0;

                    // Processes the next turn in the game simulation.
                    game.TakeTurn(false);

                    // The base game never rolls the Wild or Animal categories anywhere, so roadside
                    // America (the §6 strangers and crowds) would register but never appear. Wire both
                    // into the per-day travel tick using the same probabilistic TriggerEventByType call
                    // the other categories use; the per-category odds live in EventDirectorModule.
                    game.EventDirector.TriggerEventByType(game.Vehicle, EventCategory.Wild);
                    game.EventDirector.TriggerEventByType(game.Vehicle, EventCategory.Animal);

                    // The 2028 re-skin's headline difficulty lever: a satirical, allegorical "modern American
                    // road-trip" death or catastrophe, rolled once per moving travel day. The per-day odds live
                    // in EventDirectorModule.CategoryChance(ModernHazard); the weighted outcome spread (whole-party
                    // wipe / one-off death / maiming / supply drain) lives in the src/Event/Modern/ prefabs. Tuned
                    // in the headless balance sim so competent play wins only about half the time.
                    game.EventDirector.TriggerEventByType(game.Vehicle, EventCategory.ModernHazard);

                    // Scripted flavor: a 0.02%/turn chance (1 in 5000) that Red Dead Redemption 3 drops mid-trip
                    // and every living passenger has, without discussion, pre-ordered the $80 edition.
                    if (game.Random.Next(5000) == 0)
                        game.EventDirector.TriggerEvent(game.Vehicle, typeof(RedDeadRedemption3));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Can only stop the simulation if it is actually running.
            if (!string.IsNullOrEmpty(input))
                return;

            // Stop ticks and close this state.
            if (GameSimulationApp.Instance.Vehicle.Status == VehicleStatus.Moving)
                GameSimulationApp.Instance.Vehicle.Status = VehicleStatus.Stopped;

            // Remove the this form.
            ClearForm();
        }
    }
}