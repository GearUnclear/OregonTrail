// Created for the 2028 Asphalt Trail re-skin -- vehicle selection.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OregonTrailDotNet.Entity.Vehicle;
using OregonTrailDotNet.UI;
using OregonTrailDotNet.Window.MainMenu.Names;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.MainMenu.VehicleSelection
{
    /// <summary>
    ///     Facilitates the ability for a user to select which vehicle the party will make the drive to Seattle in. This
    ///     choice determines travel speed, fuel efficiency, cargo capacity, and the maximum party size for the rest of
    ///     the game.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class VehicleSelector : Form<NewGameInfo>
    {
        /// <summary>
        ///     References the string for the vehicle selection so it is only constructed once.
        /// </summary>
        private StringBuilder _vehicleChooser;

        /// <summary>
        ///     Set when the player tried to pick a vehicle that would floor their starting cash to $0; rendered as a
        ///     warning until they pick something affordable.
        /// </summary>
        private string _blockedMessage;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the vehicle choices.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="VehicleSelector" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public VehicleSelector(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>Default is FALSE. Setting to TRUE allows characters and input buffer to be read when submitted.</remarks>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            // Set the vehicle to default value in case we are retrying this.
            UserData.VehiclePick = VehicleChoice.Minivan;

            // Pass the game data to the simulation for each new game Windows state.
            GameSimulationApp.Instance.SetStartInfo(UserData);

            // String builder that will hold our representation of all possible vehicles player can choose from.
            _vehicleChooser = new StringBuilder();
            _vehicleChooser.AppendLine($"{Environment.NewLine}Every ride left in the driveway made");
            _vehicleChooser.AppendLine($"the drive to Seattle differently.{Environment.NewLine}");
            _vehicleChooser.Append("You may:");
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
            // Rebuilt every render pass (not just on form creation) so the arrow-key highlight stays current.
            var vehicles =
                new List<VehicleChoice>(Enum.GetValues(typeof(VehicleChoice)).Cast<VehicleChoice>());

            var options = new List<ArrowMenuOption>();
            foreach (var vehicleChoice in vehicles)
            {
                // Get the tuning numbers for this vehicle choice.
                var vehicleModel = VehicleModels.Get(vehicleChoice);

                var optionText = new StringBuilder();
                optionText.AppendLine($"{(int) vehicleChoice}. {vehicleChoice.ToDescriptionAttribute()}");
                optionText.AppendLine($"     {vehicleModel.FlavorText}");
                optionText.Append(
                    $"     Price: {vehicleModel.Cost:C0} | Seats: {vehicleModel.MaxPartySize} | Cargo: {vehicleModel.CargoCapacity} lbs");

                // Warn the player if this vehicle would wipe out their starting cash.
                if (vehicleModel.Cost >= UserData.StartingMonies)
                    optionText.Append(" (wipes out your starting cash)");

                options.Add(new ArrowMenuOption(optionText.ToString(), ((int) vehicleChoice).ToString()));
            }

            options.Add(new ArrowMenuOption(
                $"{vehicles.Count + 1}. Find out the differences between these rides",
                (vehicles.Count + 1).ToString()));

            _menu.SetOptions(options);
            GameSimulationApp.Instance.ActiveMenu = _menu;

            var rendered = _vehicleChooser + Environment.NewLine + Environment.NewLine + _menu.Render();
            return string.IsNullOrEmpty(_blockedMessage)
                ? rendered
                : rendered + Environment.NewLine + _blockedMessage;
        }

        /// <summary>
        ///     Locks in the given vehicle choice unless doing so would floor the party's starting cash to $0, in which
        ///     case the player is kept on this screen with a warning instead.
        /// </summary>
        /// <param name="vehicleChoice">The vehicle the player just picked.</param>
        private void SelectVehicle(VehicleChoice vehicleChoice)
        {
            var vehicleModel = VehicleModels.Get(vehicleChoice);
            if (vehicleModel.Cost >= UserData.StartingMonies)
            {
                _blockedMessage =
                    $"You can't afford the {vehicleModel.Name} - it would leave you with nothing to buy supplies with. Pick something else.";
                return;
            }

            _blockedMessage = null;
            UserData.VehiclePick = vehicleChoice;
            UserData.PlayerNameIndex = 0;
            SetForm(typeof(InputPlayerNames));
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Skip if the input is null or empty.
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
                return;

            // Attempt to cast string to enum value, can be characters or integer.
            Enum.TryParse(input, out VehicleChoice vehicleChoice);

            // Once a vehicle is selected, we need to confirm that is what the user wanted.
            switch (vehicleChoice)
            {
                case VehicleChoice.Minivan:
                case VehicleChoice.PickupCamper:
                case VehicleChoice.HybridCrossover:
                case VehicleChoice.ElectricHatchback:
                    SelectVehicle(vehicleChoice);
                    break;
                default:
                    SetForm(typeof(VehicleHelp));
                    break;
            }
        }
    }
}
