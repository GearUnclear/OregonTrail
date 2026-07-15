// The Asphalt Trail (2028 re-skin) -- roadside cameo: gruff Tom Holland hard-selling his BERO non-alc beer.

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A wiry, chain-smoking Tom Holland corners the party at a rest stop and aggressively pushes cans of his
    ///     real non-alcoholic beer, BERO. Unlike the one-shot Wild events, this one is a conversation: it immediately
    ///     hands rendering off to <see cref="TomHollandEncounter" />, where Tom talks over the player and a basic
    ///     sentiment pass on whatever words they blurt decides whether he "hears" a yes (and charges them for a case)
    ///     or a no (and lets them off with a single warm can). All the actual effect and dialogue live in that form;
    ///     this class exists only to register the event with the director and route to the form.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TomHollandBero : EventProduct
    {
        /// <summary>
        ///     Nothing to apply up front -- the outcome depends on the player's replies, which are gathered in the
        ///     conversational form. The interactive form does all the work in <see cref="OnPostExecute" />.
        /// </summary>
        /// <param name="eventExecutor">Source entity (the party vehicle) affected by the event.</param>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            // Intentionally empty: see class summary. The encounter form drives everything from here.
        }

        /// <summary>
        ///     Hands rendering to the conversational encounter form instead of the plain one-shot event text.
        /// </summary>
        /// <param name="eventExecutor">Form that executed the event from the random event window.</param>
        internal override bool OnPostExecute(EventExecutor eventExecutor)
        {
            base.OnPostExecute(eventExecutor);

            eventExecutor.SetForm(typeof(TomHollandEncounter));
            return true;
        }

        /// <summary>
        ///     Fallback text. The encounter form supplies its own screens, but EventExecutor requires non-empty render
        ///     text before the hand-off, so return the pitch line here.
        /// </summary>
        /// <param name="userData">Source entity information for the event.</param>
        /// <returns>Non-empty pitch string.</returns>
        protected override string OnRender(RandomEventInfo userData)
        {
            return "A man in a leather jacket steps off the shoulder waving a gold can of beer at\nyou.";
        }
    }
}
