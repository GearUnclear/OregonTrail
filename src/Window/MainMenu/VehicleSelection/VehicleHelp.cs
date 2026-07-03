// Created for the 2028 Asphalt Trail re-skin — vehicle selection.

using System;
using System.Text;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.MainMenu.VehicleSelection
{
    /// <summary>
    ///     Shows information about what each vehicle choice means and how it affects speed, fuel efficiency, cargo
    ///     capacity, and party size for the rest of the game.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class VehicleHelp : InputForm<NewGameInfo>
    {
        /// <summary>Initializes a new instance of the <see cref="VehicleHelp" /> class.</summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public VehicleHelp(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            // Information about the four vehicles, the tradeoffs each one represents.
            var ride = new StringBuilder();
            ride.AppendLine($"{Environment.NewLine}Four rides sat in the driveway this");
            ride.AppendLine($"morning. Each one trades speed, fuel,{Environment.NewLine}");
            ride.AppendLine($"cargo room, and seating for something else:{Environment.NewLine}");

            ride.AppendLine("The BEIGE MINIVAN is the boring middle");
            ride.AppendLine("ground. Average speed, average mileage,");
            ride.AppendLine($"decent room, seats four. Nothing to brag about.{Environment.NewLine}");

            ride.AppendLine("The LIFTED PICKUP WITH A CAMPER SHELL hauls");
            ride.AppendLine("the most cargo of the four and still seats");
            ride.AppendLine("four, but it is the slowest of the bunch and");
            ride.AppendLine($"burns through gas cans like they are free.{Environment.NewLine}");

            ride.AppendLine("The HYBRID CROSSOVER SUV is quick and sips");
            ride.AppendLine("fuel, but the trunk is small and there is");
            ride.AppendLine($"only room for three people total.{Environment.NewLine}");

            ride.AppendLine("The SECONDHAND EV HATCHBACK is the fastest");
            ride.AppendLine("and most fuel-efficient of the four thanks");
            ride.AppendLine("to instant torque, but it has the least cargo");
            ride.AppendLine("room and seats only three, and it still needs");
            ride.AppendLine($"a gas generator riding along in back for when{Environment.NewLine}");
            ride.AppendLine("the charging network between here and Seattle");
            ride.AppendLine($"fails to materialize.{Environment.NewLine}");

            ride.AppendLine("Speed gets you there sooner. Cargo room keeps");
            ride.AppendLine("you fed and clothed. Seats decide who gets to");
            ride.AppendLine($"come along at all. Choose accordingly.{Environment.NewLine}");
            return ride.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            SetForm(typeof(VehicleSelector));
        }
    }
}
