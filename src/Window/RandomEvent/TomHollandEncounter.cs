// The Asphalt Trail (2028 re-skin) -- the Tom Holland / BERO rest-stop encounter form.

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Renderer;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.RandomEvent
{
    /// <summary>
    ///     A conversational cameo: a gruff, chain-smoking Tom Holland corners the party at a rest stop and hard-sells
    ///     his real non-alcoholic beer, BERO. The gimmick is that he TALKS OVER the player -- every time the player
    ///     tries to reply, they get exactly one word out (the WolfCurses input layer only keeps letters/digits, so
    ///     free text arrives one bare word per ENTER anyway) before Tom steamrolls them with the next bit of his
    ///     monologue. After a few exchanges the words the player managed to blurt are run through a deliberately
    ///     basic sentiment scorer (<see cref="BeroSentiment" />); Tom then "hears" either a yes or a no and reacts
    ///     accordingly -- but a mumble or silence collapses to a yes, because a man handing out his own beer assumes
    ///     the sale. Attached by <see cref="Event.Wild.TomHollandBero" />, mirroring how PokemonCardScam hands off to
    ///     <see cref="ScammerReveal" />.
    /// </summary>
    [ParentWindow(typeof(RandomEvent))]
    public sealed class TomHollandEncounter : Form<RandomEventInfo>
    {
        /// <summary>Price of the "case" Tom charges you for if he decides you said yes. Clamped to your cash.</summary>
        private const int BeroCasePrice = 45;

        /// <summary>
        ///     Number of times the player gets to try to speak before Tom stops talking long enough to "decide"
        ///     what they said. Three attempts -- all talked over -- then the outcome.
        /// </summary>
        private const int ResolveStage = 3;

        /// <summary>Two-frame loop of Tom taking a drag and thrusting the can, animated like the scammer reveal.</summary>
        private AsciiAnimation _tom;

        /// <summary>Cached full screen text (art frame + current dialogue + prompt), rebuilt on tick and on input.</summary>
        private StringBuilder _render;

        /// <summary>The current block of Tom's dialogue; swapped as the exchange advances.</summary>
        private string _dialogue;

        /// <summary>How many times the player has hit ENTER at Tom so far. Drives which monologue shows.</summary>
        private int _stage;

        /// <summary>Every bare word the player managed to blurt, fed to the sentiment scorer at resolution.</summary>
        private readonly List<string> _words = new List<string>();

        /// <summary>True once Tom has "decided" and applied the outcome; the next ENTER then closes the encounter.</summary>
        private bool _resolved;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TomHollandEncounter" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public TomHollandEncounter(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     TRUE so every ENTER -- including a blank one -- is routed to <see cref="OnInputBufferReturned" /> as
        ///     free text (one word) rather than being parsed as a menu command. The scorer reads these words.
        /// </summary>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Fired once the form is attached. Sets up the animation and shows Tom's opening pitch.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            _tom = new AsciiAnimation(SceneArt.TomHollandA, SceneArt.TomHollandB);
            _render = new StringBuilder();
            _dialogue = OpeningPitch();
            Compose();
        }

        /// <summary>
        ///     Returns the cached text: current Tom frame on top, his dialogue below, then the prompt.
        /// </summary>
        /// <returns>The text user interface.</returns>
        public override string OnRenderForm()
        {
            return _render.ToString();
        }

        /// <summary>
        ///     Advances the two-frame lunge on fixed simulation ticks only (system ticks fire too fast). No calendar
        ///     time passes; we only flip the animation frame while the player reads.
        /// </summary>
        /// <param name="systemTick">TRUE if ticked by the OS; FALSE if pulsed at the fixed simulation interval.</param>
        /// <param name="skipDay">TRUE if force-ticked without advancing time.</param>
        public override void OnTick(bool systemTick, bool skipDay)
        {
            base.OnTick(systemTick, skipDay);

            if (systemTick)
                return;

            _tom.Step();
            Compose();
        }

        /// <summary>
        ///     Fired for each ENTER the player presses. The input layer strips everything but letters/digits, so each
        ///     submission is a single bare word (or empty). We bank it, advance the exchange, and -- once the player
        ///     has been talked over enough times -- let Tom "decide" what they said and apply the outcome.
        /// </summary>
        /// <param name="input">The single word the player got out before being cut off.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Once Tom has delivered the outcome, any key gets the party back on the road.
            if (_resolved)
            {
                ParentWindow.RemoveWindowNextTick();
                return;
            }

            // Bank whatever word the player managed to blurt (silence still counts as a turn -- he talks over it).
            var word = input?.Trim();
            if (!string.IsNullOrEmpty(word))
                _words.Add(word);

            _stage++;

            if (_stage >= ResolveStage)
                Resolve();
            else
                _dialogue = Interruption(_stage);

            Compose();
        }

        /// <summary>Rebuilds the cached screen from the current animation frame, dialogue block, and prompt.</summary>
        private void Compose()
        {
            _render.Clear();
            _render.AppendLine(_tom.CurrentFrame);
            _render.AppendLine();
            _render.AppendLine(_dialogue);
            _render.AppendLine(_resolved
                ? "Press ENTER to pull back onto the trail."
                : "You try to get a word in edgewise. Type it and press ENTER:");
        }

        /// <summary>Tom's opening pitch, shown the moment he corners you.</summary>
        private static string OpeningPitch()
        {
            var sb = new StringBuilder();
            sb.AppendLine("A wiry bloke in a beat-up leather jacket flicks a cigarette into the gravel");
            sb.AppendLine("and lights the next one off the ember. You know that face. It's Tom Holland");
            sb.AppendLine("-- not the chatty one off the red carpet. This one's got a rasp and a");
            sb.AppendLine("thousand-yard squint.");
            sb.AppendLine();
            sb.AppendLine("\"Oi. You.\" *drag* \"Don't -- don't do the 'you're shorter than I thought'");
            sb.AppendLine("thing, I've heard it. Five foot eight. It's fine. It's FINE.\"");
            sb.AppendLine();
            sb.AppendLine("He presses a cold gold can into your hands. It says BERO. Rhymes with zero.");
            sb.AppendLine("\"Non-alcoholic. Point-five percent. Kingston Golden Pils -- named after me");
            sb.AppendLine("hometown, birthplace of England, has a trout on it, the trout means balance.");
            sb.Append("Try it.\"");
            return sb.ToString();
        }

        /// <summary>The monologue Tom steamrolls you with on each attempt to reply. Stage is 1-based here.</summary>
        /// <param name="stage">Which interruption to show (1 = first time cut off).</param>
        private static string Interruption(int stage)
        {
            var sb = new StringBuilder();
            switch (stage)
            {
                case 1:
                    sb.AppendLine("\"-- nope. Nope. Stop.\" He exhales smoke through his nose. \"You were gonna");
                    sb.AppendLine("say 'isn't this just a brand deal, Tom.' It is NOT a brand deal. It's");
                    sb.AppendLine("PERSONAL. First business venture. Me mates tried it, said we'd bottled");
                    sb.AppendLine("liquid gold -- so either they're all lying to me, or...\" He trails off,");
                    sb.AppendLine("staring at the can.");
                    sb.AppendLine();
                    sb.AppendLine("\"Dry January. 2022. Spilled into February 'cause January was too hard, never");
                    sb.AppendLine("stopped. Hardest thing I've ever done. Greatest achievement of my life, and");
                    sb.Append("I've done the upside-down kiss.\"");
                    return sb.ToString();
                case 2:
                    sb.AppendLine("\"-- see, now you're TALKING, but I'm not hearing a no.\" *drag* \"Zendaya");
                    sb.AppendLine("hates beer. HATES it. Sipped this, went 'wow, these are really tasty.'");
                    sb.AppendLine("That's canon. Put it in the script. Actually don't, I'll leak it, I always");
                    sb.AppendLine("leak it, Marvel hands me fake pages now --\"");
                    sb.AppendLine();
                    sb.AppendLine("He lights another off the last one. \"There's the Edge Hill Hazy IPA, that's");
                    sb.AppendLine("me school. Noon Wheat, named after me dog Noon. Everything's named after");
                    sb.AppendLine("something small and personal, that's the whole -- the whole THING, mate,");
                    sb.Append("that's the ETHOS --\"");
                    return sb.ToString();
                default:
                    // Should not happen (stage >= ResolveStage resolves instead), but never render empty.
                    sb.Append("\"-- anyway. Where was I.\"");
                    return sb.ToString();
            }
        }

        /// <summary>
        ///     Tom stops talking long enough to "decide" what you said, then reacts. A clear no lets you off the
        ///     hook; anything else -- yes, mumbling, or dead silence -- he takes as a sale and charges you for a case.
        /// </summary>
        private void Resolve()
        {
            _resolved = true;

            var vehicle = UserData.SourceEntity as Entity.Vehicle.Vehicle ??
                          GameSimulationApp.Instance.Vehicle;

            switch (BeroSentiment.Score(_words))
            {
                case BeroLean.Reject:
                    _dialogue = RejectOutcome();
                    break;
                default:
                    // Accept and Neutral both land as a yes -- he assumes the sale and talks over any hesitation.
                    _dialogue = AcceptOutcome(ChargeForCase(vehicle));
                    break;
            }
        }

        /// <summary>Charges up to <see cref="BeroCasePrice" />, but never more cash than the party actually has.</summary>
        /// <param name="vehicle">Party vehicle whose cash slot pays for the case.</param>
        /// <returns>The dollars actually taken, for honest outcome text.</returns>
        private static int ChargeForCase(Entity.Vehicle.Vehicle vehicle)
        {
            if (vehicle == null)
                return 0;

            var cash = vehicle.Inventory[Entities.Cash];
            var charged = Math.Min(BeroCasePrice, cash.Quantity);
            if (charged > 0)
                cash.ReduceQuantity(charged);
            return charged;
        }

        /// <summary>Outcome when Tom decides you said yes: you leave poorer, holding beer you can't eat.</summary>
        /// <param name="charged">Dollars he took off you for the case.</param>
        private static string AcceptOutcome(int charged)
        {
            var sb = new StringBuilder();
            sb.AppendLine("\"YES. THERE it is. I knew you were one of the good ones.\" Before you can");
            sb.AppendLine("move he claps a whole case of gold cans into your arms. \"Expect nothing");
            sb.AppendLine("less. Well-being's holistic, yeah? Moderation. Discernment. Nine a.m. tee");
            sb.AppendLine("time and I'm fresh as a daisy.\" He's already walking off. \"THE TROUT MEANS");
            sb.AppendLine("BALANCE!\"");
            sb.AppendLine();
            if (charged > 0)
            {
                sb.AppendLine("You are holding a case of non-alcoholic beer you did not agree to buy. It");
                sb.AppendLine("weighs on the axle and nourishes no one. Somewhere in there he lifted");
                sb.Append($"${charged} off you.");
            }
            else
            {
                sb.AppendLine("You are holding a case of non-alcoholic beer you did not agree to buy. It");
                sb.AppendLine("weighs on the axle and nourishes no one. You're flat broke, so at least it");
                sb.Append("was free.");
            }
            return sb.ToString();
        }

        /// <summary>Outcome when the scorer heard a clear no: Tom deflates, leaves one can, and you escape clean.</summary>
        private static string RejectOutcome()
        {
            var sb = new StringBuilder();
            sb.AppendLine("The word lands. He goes quiet. The cigarette burns down to his knuckles and");
            sb.AppendLine("he doesn't flinch. \"...Right. No. That's -- that's fine. That's actually");
            sb.AppendLine("fine.\" *long drag* \"You know who else said no? My real mates. Turns out the");
            sb.AppendLine("only thing we had in common was that I really liked drinking. Funny how that");
            sb.AppendLine("shakes out.\"");
            sb.AppendLine();
            sb.AppendLine("He sets a single gold can on your hood, gentle, like it's a wounded bird.");
            sb.AppendLine("\"Just take the one. For the road.\" He walks off into the dark muttering");
            sb.AppendLine("\"...unlucky.\" You lost twenty minutes and gained one warm can of nothing.");
            sb.Append("Your wallet is intact.");
            return sb.ToString();
        }
    }
}
