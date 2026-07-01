using System;
using System.Text;
using OregonTrailDotNet.Entity;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Decision
{
    /// <summary>
    ///     "The Others at Carhenge" — the party arrives at the junked-sedan monument and meets Reyes' caravan. The player
    ///     chooses whether to join the convoy (slower, shared food buy-in, safety and the best endgame) or break off and go
    ///     solo (faster, keeps all food, exposed). Reads the earlier "pack" decision to scale the buy-in and penalties.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class CaravanDecision : Form<TravelInfo>
    {
        /// <summary>
        ///     Holds representation of the caravan decision as text presented to the player.
        /// </summary>
        private readonly StringBuilder _decisionPrompt;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CaravanDecision" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public CaravanDecision(IWindow window) : base(window)
        {
            _decisionPrompt = new StringBuilder();
        }

        /// <summary>
        ///     Required so free-text/numeric input is routed to <see cref="OnInputBufferReturned" />; without it input never
        ///     arrives.
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
            var pack = GameSimulationApp.Instance.Choices.GetDecision("pack");

            _decisionPrompt.Clear();
            _decisionPrompt.AppendLine($"{Environment.NewLine}The Others at Carhenge{Environment.NewLine}");
            _decisionPrompt.AppendLine(
                "Thirty junked Detroit sedans stand nose-down in the Nebraska dirt like a Stonehenge for a religion that");
            _decisionPrompt.AppendLine(
                "already failed. Camped in their shadow: forty families who read the same weather app you did.");
            _decisionPrompt.AppendLine(string.Empty);
            _decisionPrompt.AppendLine(
                "A woman named Reyes runs the caravan — shared fuel, shared watch, shared food pot, half the speed.");
            _decisionPrompt.AppendLine("Nobody crosses the empty middle alone.");

            if (pack == "heavy")
            {
                _decisionPrompt.AppendLine(string.Empty);
                _decisionPrompt.AppendLine(
                    "They clock your overloaded rig and the copper poking out the back — a rolling yard sale, and every");
                _decisionPrompt.AppendLine("pound of it their problem if you throw a rod. The price of admission goes up.");
            }
            else if (pack == "teen")
            {
                _decisionPrompt.AppendLine(string.Empty);
                _decisionPrompt.AppendLine(
                    "Mateo already knows two kids here from the group chat; an older woman nods at you like you've passed a test.");
            }

            _decisionPrompt.AppendLine(string.Empty);
            _decisionPrompt.AppendLine("  1. Join the caravan");
            _decisionPrompt.Append("  2. Break off and go solo — faster, exposed");

            return _decisionPrompt.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game mode.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Parse the user input buffer as integer.
            if (!int.TryParse(input, out var parsedInputNumber))
                return;

            var game = GameSimulationApp.Instance;
            var vehicle = game.Vehicle;
            var pack = game.Choices.GetDecision("pack");

            switch (parsedInputNumber)
            {
                case 1:
                    // Join the caravan: food goes into a shared pool. Heavy hoarders get taxed harder, teen
                    // contacts waive most of the buy-in and the convoy shares back (morale heal).
                    var buyIn = pack == "heavy" ? 100 : (pack == "teen" ? 20 : 60);
                    vehicle.Inventory[Entities.Food].ReduceQuantity(buyIn);
                    if (pack == "teen")
                        foreach (var passenger in vehicle.Passengers)
                            passenger.HealEntirely();

                    game.Choices.Record("caravan", "join", 600,
                        "You gave the pool your surplus and your speed, and when the axle finally snapped in Wyoming there were forty strangers who stopped — a debt the hoarders behind you never got to call in.");
                    ClearForm();
                    break;

                case 2:
                    // Break off and go solo: keep all food, but no lookout and no safety net — one ambush costs
                    // the party health. The overloaded rig was always a caravan liability, so a heavy pack is spared.
                    if (pack != "heavy")
                        foreach (var passenger in vehicle.Passengers)
                            passenger.Damage(15);

                    game.Choices.Record("caravan", "solo", -150,
                        "You made better time than the convoy and owed no one, right up until the road went quiet at 3 a.m. and the whole family learned exactly how alone 'independent' feels.");
                    ClearForm();
                    break;

                default:
                    // Invalid input: the form simply re-renders.
                    return;
            }
        }
    }
}
