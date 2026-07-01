// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Renderer;
using WolfCurses.Core;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.RandomEvent
{
    /// <summary>
    ///     Renders the two-frame ASCII loop of the graded-card grifter leering and thrusting his "slab" at the party
    ///     while the outcome text sits underneath. The frames flip on every fixed simulation tick so the creep appears
    ///     to lunge in place; no calendar time passes while the player reads. Attached by <see cref="Event.Wild.PokemonCardScam" />.
    /// </summary>
    [ParentWindow(typeof(RandomEvent))]
    public sealed class ScammerReveal : Form<RandomEventInfo>
    {
        /// <summary>
        ///     The looping grifter animation. Two frames that alternate to fake motion, mirroring how the river shimmer
        ///     is animated in the travel window.
        /// </summary>
        private AsciiAnimation _grifter;

        /// <summary>
        ///     Cached text user interface rebuilt whenever the animation advances so the render call stays cheap.
        /// </summary>
        private StringBuilder _reveal;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScammerReveal" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public ScammerReveal(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>Default is FALSE. Setting to TRUE allows characters and input buffer to be read when submitted.</remarks>
        public override bool InputFillsBuffer => false;

        /// <summary>
        ///     Determines if this dialog state is allowed to receive any input at all, even empty line returns. Always true so a
        ///     single Enter closes the reveal and returns the party to the trail; the animation keeps looping until then.
        /// </summary>
        public override bool AllowInput => true;

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            _grifter = new AsciiAnimation(SceneArt.PokemonScammerA, SceneArt.PokemonScammerB);
            _reveal = new StringBuilder();
            BuildReveal();
        }

        /// <summary>
        ///     Rebuilds the cached text: current animation frame on top, the event's outcome text below, then the prompt.
        /// </summary>
        private void BuildReveal()
        {
            _reveal.Clear();
            _reveal.AppendLine(_grifter.CurrentFrame);

            if (!string.IsNullOrEmpty(UserData.EventText))
                _reveal.AppendLine($"{Environment.NewLine}{UserData.EventText}");

            _reveal.AppendLine($"{Environment.NewLine}{InputManager.PRESSENTER}{Environment.NewLine}");
        }

        /// <summary>
        ///     Returns a text only representation of the current game Windows state.
        /// </summary>
        /// <returns>The text user interface.<see cref="string" />.</returns>
        public override string OnRenderForm()
        {
            return _reveal.ToString();
        }

        /// <summary>
        ///     Called when the simulation is ticked by underlying operating system, game engine, or potato.
        /// </summary>
        /// <param name="systemTick">TRUE if ticked unpredictably by the OS; FALSE if pulsed at the fixed simulation interval.</param>
        /// <param name="skipDay">Determines if the simulation force ticked without advancing time or down the trail.</param>
        public override void OnTick(bool systemTick, bool skipDay)
        {
            base.OnTick(systemTick, skipDay);

            // Only advance the leer on fixed simulation ticks; system ticks fire too fast and unpredictably. No calendar
            // time is consumed here, we only flip the animation frame while the player reads the outcome.
            if (systemTick)
                return;

            _grifter.Step();
            BuildReveal();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Any acknowledgement closes the whole random event and drops the party back onto the trail.
            ParentWindow.RemoveWindowNextTick();
        }
    }
}
