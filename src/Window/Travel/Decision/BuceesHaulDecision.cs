using System;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.UI;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Decision
{
    /// <summary>
    ///     Buc-ee's, Sevierville -- "The Cathedral of Snacks." A pure money/food/time triangle: blow the budget on a
    ///     mega-haul, stay disciplined, or take a shady pallet gig. No option dominates. Fires once when the party arrives
    ///     at the location; records the outcome in the per-game <see cref="Module.Choices.ChoiceLedger" />.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class BuceesHaulDecision : Form<TravelInfo>
    {
        /// <summary>
        ///     Holds the rendered decision prompt and numbered menu.
        /// </summary>
        private readonly StringBuilder _prompt;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the three haul options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="BuceesHaulDecision" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public BuceesHaulDecision(IWindow window) : base(window)
        {
            _prompt = new StringBuilder();
        }

        /// <summary>
        ///     Required so the free-text/numeric input actually reaches <see cref="OnInputBufferReturned" />.
        /// </summary>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Returns the non-empty numbered menu shown to the player.
        /// </summary>
        /// <returns>The <see cref="string" />.</returns>
        public override string OnRenderForm()
        {
            _prompt.Clear();
            _prompt.AppendLine($"{Environment.NewLine}The Cathedral of Snacks{Environment.NewLine}");
            _prompt.AppendLine(
                "One hundred twenty fuel pumps and a wall of jerky the length of a football field. The sixty-foot beaver on the sign smiles down like a saint who knows something you don't.");
            _prompt.AppendLine(string.Empty);
            _prompt.AppendLine(
                "Inside: an acre of brisket, a chapel-quiet hush over the fudge, and the last functioning bathrooms in three states.");
            _prompt.AppendLine(string.Empty);
            _prompt.AppendLine(
                "A man in a vest is hiring drivers to run unmarked pallets to Knoxville, cash same-day, no questions. Your wallet, your pantry, and your calendar can each afford to win exactly once here.");
            _prompt.AppendLine(string.Empty);

            _menu.SetOptions(new[]
            {
                new ArrowMenuOption("1. Blow the budget on a mega-haul -- brisket, water, the good jerky", "1"),
                new ArrowMenuOption("2. Stay disciplined -- top off the tank, grab a little, keep the powder dry", "2"),
                new ArrowMenuOption("3. Take the pallet gig -- haul freight to Knoxville and back", "3")
            });
            GameSimulationApp.Instance.ActiveMenu = _menu;
            _prompt.Append(_menu.Render());
            return _prompt.ToString();
        }

        /// <summary>
        ///     Fired when the player types a response into the input buffer.
        /// </summary>
        /// <param name="input">Contents of the input buffer.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Parse the user input buffer as integer; ignore anything unparsable.
            if (!int.TryParse(input, out var parsedInputNumber))
                return;

            var game = GameSimulationApp.Instance;
            var vehicle = game.Vehicle;

            switch (parsedInputNumber)
            {
                case 1:
                    // -$400 cash, +300 food.
                    vehicle.Inventory[Entities.Cash].ReduceQuantity(400);
                    vehicle.Inventory[Entities.Food].AddQuantity(300);
                    game.Choices.Record("bucees", "haul", 250,
                        "You bankrupted yourself under the beaver's grin, and still had brisket in Kansas when others were boiling shoe leather.");
                    break;
                case 2:
                    // -$40 cash (fuel), +80 food; the cash cushion survives.
                    vehicle.Inventory[Entities.Cash].ReduceQuantity(40);
                    vehicle.Inventory[Entities.Food].AddQuantity(80);
                    game.Choices.Record("bucees", "disciplined", 300,
                        "You walked past the cathedral with your wallet shut, and the folded money you kept was the reason a checkpoint later was pocket change.");
                    break;
                case 3:
                    // +$500 cash but -3 days on the detour and -20 health to the party.
                    vehicle.Inventory[Entities.Cash].AddQuantity(500);
                    for (var i = 0; i < 3; i++)
                        game.Time.TickTime(false);
                    foreach (var passenger in vehicle.Passengers)
                        passenger.Damage(20);
                    game.Choices.Record("bucees", "gig", -200,
                        "The pallets were somebody else's problem by Thursday and the cash was real, but the three days you burned were three days the season never gave back.");
                    break;
                default:
                    // Invalid selection re-renders the form.
                    return;
            }

            // Return to the Travel menu.
            ClearForm();
        }
    }
}
