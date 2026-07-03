// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.UI;
using OregonTrailDotNet.Window.Travel.Command;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Decision
{
    /// <summary>
    ///     Tone-setting departure decision fired the first time the party reaches Cape Coral. The player decides what fits in
    ///     the SUV as the tide rises: load up on resellables (the "heavy" flag), travel light with meds and clean papers (the
    ///     "light"/documented flag), or leave a seat for the Delgado kid (the "teen" flag). Each option applies an immediate
    ///     inventory/health/roster effect and records a flag + score delta + epilogue line in the ChoiceLedger under the
    ///     fixed decision key "pack".
    /// </summary>
    [ParentWindow(typeof(OregonTrailDotNet.Window.Travel.Travel))]
    public sealed class PackTheCarDecision : Form<TravelInfo>
    {
        /// <summary>
        ///     Holds the rendered representation of the decision as a numbered menu for the player.
        /// </summary>
        private readonly StringBuilder _decisionPrompt;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the three packing options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="PackTheCarDecision" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public PackTheCarDecision(IWindow window) : base(window)
        {
            _decisionPrompt = new StringBuilder();
        }

        /// <summary>
        ///     Required so the free-text/numeric input actually reaches <see cref="OnInputBufferReturned" />.
        /// </summary>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Returns a text only representation of the current game Windows state.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string OnRenderForm()
        {
            _decisionPrompt.Clear();
            _decisionPrompt.AppendLine($"{Environment.NewLine}What Fits in the SUV{Environment.NewLine}");
            _decisionPrompt.AppendLine(
                "The county's drone already tagged the house UNINSURABLE / CONDEMNED and moved on down the flooded cul-de-sac; the driveway is a boat ramp now and the water is at the third porch step.");
            _decisionPrompt.AppendLine(
                "One SUV with a bad rear axle, forty minutes before the tide and the repo truck. Whatever you leave, someone with a crowbar inherits by Tuesday.");
            _decisionPrompt.AppendLine(
                "Mrs. Delgado two doors down didn't make the last convoy. Her boy Mateo is on your lawn with a duffel and no plan.");
            _decisionPrompt.AppendLine(string.Empty);

            _menu.SetOptions(new[]
            {
                new ArrowMenuOption(
                    "1. Cram it with resellables - the flatscreen, the good copper, Grandpa's flatware to flip for gas money",
                    "1"),
                new ArrowMenuOption(
                    "2. Travel light - insulin, the kids' inhalers, birth certificates, the deed nobody will honor",
                    "2"),
                new ArrowMenuOption("3. Leave a seat for the Delgado kid - take Mateo", "3")
            });
            GameSimulationApp.Instance.ActiveMenu = _menu;
            _decisionPrompt.Append(_menu.Render());

            return _decisionPrompt.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game mode.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Parse the user input buffer as integer; invalid input just re-renders the form.
            if (!int.TryParse(input, out var parsedInputNumber))
                return;

            var vehicle = GameSimulationApp.Instance.Vehicle;

            switch (parsedInputNumber)
            {
                case 1:
                    // Cram it full of resellables: cash now, but lose pantry room and ride overloaded.
                    vehicle.Inventory[Entities.Cash].AddQuantity(600);
                    vehicle.Inventory[Entities.Food].ReduceQuantity(80);
                    GameSimulationApp.Instance.Choices.Record(
                        "pack",
                        "heavy",
                        -600,
                        "You pawned your mother's flatware at a Fort Myers strip mall for diesel, and rode low on the axles the whole way while strangers smelled the hoard on you before you spoke.");
                    // Pack fires as the party departs Cape Coral, so resume the drive rather than dropping to the menu.
                    SetForm(typeof(ContinueOnTrail));
                    break;

                case 2:
                    // Travel light: no cash, but the party carries its meds and clean papers.
                    foreach (var person in vehicle.Passengers)
                        person.HealEntirely();
                    GameSimulationApp.Instance.Choices.Record(
                        "pack",
                        "light",
                        400,
                        "You left with less than a car should hold and every paper the checkpoints ever asked for, and nobody in the family died of a thing a pharmacy could have fixed.");
                    // Pack fires as the party departs Cape Coral, so resume the drive rather than dropping to the menu.
                    SetForm(typeof(ContinueOnTrail));
                    break;

                case 3:
                    // Leave a seat for Mateo: an extra mouth, but an extra pair of hands. Only if the chosen vehicle
                    // actually has an open seat left — the 3-seat Hybrid/EV can easily already be full.
                    if (vehicle.Passengers.Count < vehicle.MaxPartySize)
                    {
                        var leaderProfession = vehicle.PassengerLeader?.Profession ?? Profession.Farmer;
                        vehicle.AddPerson(new Person(leaderProfession, "Mateo Delgado", false));
                        GameSimulationApp.Instance.Choices.Record(
                            "pack",
                            "teen",
                            300,
                            "Mateo ate his share and then some, but he changed the tire outside Amarillo in nine minutes flat, and you never once regretted the seat you filled.");
                    }
                    else
                    {
                        GameSimulationApp.Instance.Choices.Record(
                            "pack",
                            "teen_declined",
                            0,
                            "There wasn't a seat to give him -- the SUV was already packed to its limit with your own family, and you watched him wave from the curb as you pulled out.");
                    }
                    // Pack fires as the party departs Cape Coral, so resume the drive rather than dropping to the menu.
                    SetForm(typeof(ContinueOnTrail));
                    break;

                default:
                    // Invalid selection: form re-renders.
                    return;
            }
        }
    }
}
