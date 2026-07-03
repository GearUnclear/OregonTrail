using System;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.UI;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Decision
{
    /// <summary>
    ///     Morally ambiguous sporting-goods decision fired once when the party arrives at the Open-Carry Walmart. The player
    ///     chooses whether to buy a pistol, refuse on principle, or split the difference with a trauma kit and vest. The
    ///     recorded "arm" flag ("armed"/"unarmed"/"kit") is later read at the Portland checkpoint via
    ///     <see cref="OregonTrailDotNet.Module.Choices.ChoiceLedger.GetDecision" />.
    /// </summary>
    [ParentWindow(typeof(OregonTrailDotNet.Window.Travel.Travel))]
    public sealed class ArmYourselfDecision : Form<TravelInfo>
    {
        /// <summary>
        ///     Holds the rendered representation of the decision prompt and its numbered options.
        /// </summary>
        private readonly StringBuilder _armPrompt;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the three arm-yourself options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArmYourselfDecision" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public ArmYourselfDecision(IWindow window) : base(window)
        {
            _armPrompt = new StringBuilder();
        }

        /// <summary>
        ///     Required so free-text/numeric input is routed into <see cref="OnInputBufferReturned" />; without this the input
        ///     buffer is never returned to the form.
        /// </summary>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Returns a text only representation of the current game Windows state. Could be a statement, information, question
        ///     waiting input, etc.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string OnRenderForm()
        {
            _armPrompt.Clear();
            _armPrompt.AppendLine($"{Environment.NewLine}Sporting Goods, Aisle 12{Environment.NewLine}");
            _armPrompt.AppendLine(
                "The greeter has a sidearm and a lanyard. The cashier has a rifle. Half the carts have a long gun laid across the child seat, and a cardboard cutout of a smiling deputy says GEAR UP, PATRIOT.");
            _armPrompt.AppendLine(string.Empty);
            _armPrompt.AppendLine(
                "The ammo case is fuller than the pharmacy, which is empty. A pistol and two boxes of shells cost less than the insulin you couldn't buy.");
            _armPrompt.AppendLine(string.Empty);
            _armPrompt.AppendLine(
                "Whatever you carry into the Cascades, the people at the checkpoints will notice - and remember. No option here is clean, and everyone in line knows it.");
            _armPrompt.AppendLine(string.Empty);

            _menu.SetOptions(new[]
            {
                new ArrowMenuOption("1. Buy the pistol and a box of shells", "1"),
                new ArrowMenuOption("2. Refuse on principle - walk out empty-handed", "2"),
                new ArrowMenuOption("3. Buy the trauma kit and a vest instead - split the difference", "3")
            });
            GameSimulationApp.Instance.ActiveMenu = _menu;
            _armPrompt.Append(_menu.Render());
            return _armPrompt.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game mode.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Parse the user input buffer as integer; invalid input just re-renders.
            if (!int.TryParse(input, out var parsedInputNumber))
                return;

            var game = GameSimulationApp.Instance;

            switch (parsedInputNumber)
            {
                case 1:
                    // Buy the pistol and a box of shells: -$300 cash, records the "armed" flag.
                    game.Vehicle.Inventory[Entities.Cash].ReduceQuantity(300);
                    game.Choices.Record("arm", "armed", -300,
                        "You slept a little better with the weight in the glovebox and worried a lot more every time a light flashed behind you; the gun rode west as a question the family never agreed on.");
                    ClearForm();
                    break;
                case 2:
                    // Refuse on principle: no cost, records the "unarmed" flag.
                    game.Choices.Record("arm", "unarmed", 200,
                        "You kept your hands empty on principle, and at the checkpoint that principle was either the only thing that saved you or the reason they searched you for an hour - depending on who you'd chosen to travel with.");
                    ClearForm();
                    break;
                case 3:
                    // Split the difference: -$120 cash, heal the whole party, records the "kit" flag.
                    game.Vehicle.Inventory[Entities.Cash].ReduceQuantity(120);
                    foreach (var passenger in game.Vehicle.Passengers)
                        passenger.HealEntirely();
                    game.Choices.Record("arm", "kit", 50,
                        "You bought the thing that saves a life instead of the thing that takes one, and the vest stopped exactly one thing on the whole trip - which is one more than the gun stopped for anyone honest about it.");
                    ClearForm();
                    break;
            }
        }
    }
}
