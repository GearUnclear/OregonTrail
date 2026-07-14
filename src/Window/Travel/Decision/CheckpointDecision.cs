using System;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.UI;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Decision
{
    /// <summary>
    ///     "The Cascadia Line" -- the party reaches the improvised Portland/Seattle border checkpoint. This is the LATE
    ///     payoff decision and carries the largest score swing in the game. It reads BOTH the earlier "arm" and "caravan"
    ///     decisions (and the "pack" documented flag) to resolve its outcomes: the Assert option pays out on the
    ///     armed/unarmed x join/solo combination, ranging from a stripped-and-detained -1500 up to a barrier-lifting +2500.
    ///     The flat scoreDelta values in the spec are base/modal; the real matrix magnitudes are computed here and passed to
    ///     <see cref="OregonTrailDotNet.Module.Choices.ChoiceLedger.Record" /> so the endgame tabulation moves accordingly.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class CheckpointDecision : Form<TravelInfo>
    {
        /// <summary>
        ///     Holds representation of the checkpoint decision as text presented to the player.
        /// </summary>
        private readonly StringBuilder _decisionPrompt;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the three checkpoint options.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="CheckpointDecision" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        public CheckpointDecision(IWindow window) : base(window)
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
            var armed = GameSimulationApp.Instance.Choices.GetDecision("arm") == "armed";
            var join = GameSimulationApp.Instance.Choices.GetDecision("caravan") == "join";

            _decisionPrompt.Clear();
            _decisionPrompt.AppendLine($"{Environment.NewLine}The Cascadia Line{Environment.NewLine}");
            _decisionPrompt.AppendLine(
                "Portland's northern edge doesn't call itself a border. The jersey barriers, the plate scanners, the");
            _decisionPrompt.AppendLine(
                "drones, and the volunteers in orange vests with rifles call it a border. Seattle is on the far side.");
            _decisionPrompt.AppendLine(string.Empty);
            _decisionPrompt.AppendLine(
                "They're admitting 'verified households' only. Your paperwork is Florida paperwork for a Florida that's");
            _decisionPrompt.AppendLine(
                "underwater, and they want registration, inspection, and a 'resettlement processing fee.'");

            if (armed)
            {
                _decisionPrompt.AppendLine(string.Empty);
                _decisionPrompt.AppendLine(
                    "The pistol is under the back seat. They will find it if they look.");
            }

            _decisionPrompt.AppendLine(string.Empty);
            if (join)
                _decisionPrompt.AppendLine(
                    "Your forty families are stacked up behind you, engines idling in solidarity. What happens in the next");
            else
                _decisionPrompt.AppendLine(
                    "You're alone at the barrier with no one to vouch for you. What happens in the next");
            _decisionPrompt.AppendLine("five minutes was decided a thousand miles ago.");

            _decisionPrompt.AppendLine(string.Empty);

            _menu.SetOptions(new[]
            {
                new ArrowMenuOption("1. Comply -- papers out, trunk open, submit to the search", "1"),
                new ArrowMenuOption("2. Assert -- stand on your rights and demand passage", "2"),
                new ArrowMenuOption("3. Run the backroad around the line", "3")
            });
            GameSimulationApp.Instance.ActiveMenu = _menu;
            _decisionPrompt.Append(_menu.Render());

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

            // Read the earlier choices this decision pays out on.
            var armed = game.Choices.GetDecision("arm") == "armed";
            var documented = game.Choices.GetDecision("pack") == "light";
            var join = game.Choices.GetDecision("caravan") == "join";

            switch (parsedInputNumber)
            {
                case 1:
                    // Comply: lose 2 days to processing (base +400). Documented households get a priority lane
                    // (+300). An armed rig means the pistol is confiscated, a -$200 fine, and a name logged in red (-600).
                    game.Time.TickTime(false);
                    game.Time.TickTime(false);

                    var complyDelta = 400;
                    if (documented)
                        complyDelta += 300;
                    if (armed)
                    {
                        vehicle.Inventory[Entities.Cash].ReduceQuantity(200);
                        complyDelta -= 600;
                    }

                    game.Choices.Record("checkpoint", "comply", complyDelta,
                        "You handed over every document and every fear, and they stamped you a resident of a state still deciding whether it wanted you.");
                    ClearForm();
                    break;

                case 2:
                    // Assert: THE BIG FORK, resolved by arm AND caravan.
                    int assertDelta;
                    string assertEpilogue;
                    if (armed && join)
                    {
                        // An armed forty-strong caravan is not searched; the militia lifts the barrier.
                        assertDelta = 2500;
                        assertEpilogue =
                            "Forty families and one rifle apiece -- the checkpoint captain found urgent reasons to lift the barrier himself.";
                    }
                    else if (join)
                    {
                        // A nonviolent mass sit-in shames the volunteers into opening the gate.
                        assertDelta = 900;
                        assertEpilogue =
                            "Forty families sat down in the road, unarmed, until the volunteers in orange vests were too ashamed to keep the gate closed.";
                    }
                    else if (armed)
                    {
                        // A lone armed stranger nearly gets shot, passes barely.
                        foreach (var passenger in vehicle.Passengers)
                            passenger.Damage(40);
                        assertDelta = 300;
                        assertEpilogue =
                            "A lone armed stranger at the barrier -- you passed, barely, and never learned how close the orange vests came to firing.";
                    }
                    else
                    {
                        // Unarmed/kit and solo: detained, rig stripped.
                        vehicle.Inventory[Entities.Food].ReduceQuantity(150);
                        for (var i = 0; i < 4; i++)
                            game.Time.TickTime(false);
                        assertDelta = -1500;
                        assertEpilogue =
                            "Alone and unarmed, you were exactly the kind of person the Cascadia line was built to break.";
                    }

                    game.Choices.Record("checkpoint", "assert", assertDelta, assertEpilogue);
                    ClearForm();
                    break;

                case 3:
                    // Run the backroad: -3 days and -50 food slipping around on logging roads (base -200). If you were
                    // riding with the caravan, peeling off ABANDONS the convoy that vouched for you: an extra -800 and the
                    // +600 caravan-join bonus is negated (so -1400 on top of the base).
                    for (var i = 0; i < 3; i++)
                        game.Time.TickTime(false);
                    vehicle.Inventory[Entities.Food].ReduceQuantity(50);

                    var runDelta = -200;
                    if (join)
                        runDelta += -800 - 600;

                    game.Choices.Record("checkpoint", "run", runDelta,
                        "You slipped around the Cascadia line on a logging road and told no one where you'd been; if the caravan had vouched for you, their forty horns did not sound when you left them at the last exit.");
                    ClearForm();
                    break;

                default:
                    // Invalid selection: the form simply re-renders.
                    return;
            }
        }
    }
}
