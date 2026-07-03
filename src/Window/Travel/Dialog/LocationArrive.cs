// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.Renderer;
using OregonTrailDotNet.UI;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.Dialog
{
    /// <summary>
    ///     State that is attached when the event is fired for reaching a new point of interest on the trail. Default action is
    ///     to ask the player if they would like to look around, but there is a chance for this behavior to be overridden in
    ///     the constructor there is a default boolean value to skip the question asking part and force a look around event to
    ///     occur without player consent.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class LocationArrive : InputForm<TravelInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="LocationArrive" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public LocationArrive(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Defines what type of dialog this will act like depending on this enumeration value. Up to implementation to define
        ///     desired behavior.
        /// </summary>
        protected override DialogType DialogType => GameSimulationApp.Instance.Trail.IsFirstLocation
            ? DialogType.Prompt
            : DialogType.YesNo;

        /// <summary>
        ///     Tracks the arrow-key highlighted line between the Yes/No options when a look-around choice is offered.
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

            // Vehicle is stopped when you are looking around.
            GameSimulationApp.Instance.Vehicle.Status = VehicleStatus.Stopped;

            _promptText = OnDialogPrompt();
        }

        /// <summary>
        ///     Returns a text only representation of the current game Windows state. When the dialog is a real Yes/No
        ///     choice (not the first-location prompt-only message) an arrow-navigable menu is appended below the text.
        /// </summary>
        public override string OnRenderForm()
        {
            var text = _promptText;

            if (GameSimulationApp.Instance.Trail.IsFirstLocation)
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
            // Grab instance of game simulation for easy reading.
            var game = GameSimulationApp.Instance;
            var pointReached = new StringBuilder();

            // Build up representation of arrival to new location, depending on location it can change.
            if (game.Trail.IsFirstLocation)
            {
                // First point of interest has slightly different message about pulling out.
                pointReached.AppendLine(
                    $"{Environment.NewLine}Pulling out of the driveway in {game.Time.CurrentYear}...{Environment.NewLine}");
            }
            else if (game.Trail.LocationIndex < game.Trail.Locations.Count)
            {
                // Show scene art for landmarks/settlements that have it, so each place feels distinct.
                var locationArt = SceneArt.ForLocation(game.Trail.CurrentLocation.Name);
                if (locationArt != null)
                    pointReached.AppendLine($"{Environment.NewLine}{locationArt}");

                // Build up message about location the player is arriving at.
                pointReached.AppendLine(
                    $"{Environment.NewLine}You are now at the {game.Trail.CurrentLocation.Name}.");

                // A line of flavor gives each place character beyond its name.
                var flavor = ArrivalFlavor(game.Trail.CurrentLocation.Name);
                if (flavor != null)
                    pointReached.AppendLine(flavor);

                pointReached.Append("Would you like to look around? Y/N");
            }

            // Wait for input on deciding if we should take a look around.
            return pointReached.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // First location we will always clear state back to location since it is starting point.
            if (GameSimulationApp.Instance.Trail.IsFirstLocation)
            {
                ClearForm();
                return;
            }

            // Subsequent locations will confirm what the player wants to do, they can stop or keep going on the trail at their own demise.
            switch (reponse)
            {
                case DialogResponse.Custom:
                case DialogResponse.No:
                    var travelMode = ParentWindow as Travel;
                    if (travelMode == null)
                        throw new InvalidCastException(
                            "Unable to cast parent game Windows into travel game Windows when it should be that!");

                    // Call the continue on trail method command inside that game Windows, it will trigger the next action accordingly.
                    travelMode.ContinueOnTrail();
                    break;
                case DialogResponse.Yes:
                    ClearForm();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reponse), reponse, null);
            }
        }

        /// <summary>
        ///     A one-line flavor blurb for arriving at a named location, giving each place character beyond its
        ///     name. Returns null for locations without a blurb (rivers, forks, and the start are handled elsewhere).
        /// </summary>
        /// <param name="locationName">Name of the location being arrived at.</param>
        /// <returns>Flavor text, or null.</returns>
        private static string ArrivalFlavor(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
                return null;
            if (locationName.Contains("Buc-ee"))
                return "120 fuel pumps, a wall of brisket, and ammo by the register. Bladder relief at last.";
            if (locationName.Contains("Touchdown Jesus"))
                return "Sixty-odd feet of fiberglass Savior, both arms up like He just scored.";
            if (locationName.Contains("Wall Drug"))
                return "Free ice water and five-cent coffee, advertised on billboards for 500 miles.";
            if (locationName.Contains("Carhenge"))
                return "Detroit steel stood on end in a prairie circle. The druids would not understand.";
            if (locationName.Contains("Cadillac"))
                return "Ten Caddies planted nose-down and repainted daily. Spray cans encouraged.";
            if (locationName.Contains("Big Texan"))
                return "The 72-ounce steak is free if you finish it in an hour. Most do not.";
            if (locationName.Contains("Butter Cow"))
                return "A life-size cow sculpted from 600 pounds of butter, kept in a chilled case.";
            if (locationName.Contains("Walmart"))
                return "Rollback prices, open carry, and a greeter who has seen absolutely everything.";
            if (locationName.Contains("Portland"))
                return "Rain, espresso, and bumper stickers. Everyone is very calm about everything.";
            if (locationName.Contains("Tacoma"))
                return "Hand-lettered signs to the horizon. Honking means solidarity here.";
            if (locationName.Contains("Seattle"))
                return "Rain, espresso, and a 600-foot Needle. You have reached the far edge of the map.";
            return null;
        }
    }
}