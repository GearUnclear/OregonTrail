// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Module.Director;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A twitchy grifter corners the party in a gas-station lot waving a "graded" slabbed trading card he swears is
    ///     worth a fortune. It is a lopsided bet: one time in ten the slab is genuine and the party flips it for $800,
    ///     but nine times in ten it is a resealed fake with a laser-printed label and the party is out $200. The reveal
    ///     is shown over an animated ASCII loop of the creep leering and thrusting the slab at the viewer.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class PokemonCardScam : EventProduct
    {
        /// <summary>Cash swing if the slab turns out to be genuine.</summary>
        private const int RealCardPayout = 800;

        /// <summary>Cash lost when the slab is a resealed fake, which is the overwhelmingly likely outcome.</summary>
        private const int FakeCardLoss = 200;

        /// <summary>Holds the pitch plus outcome text so render is never empty even before execution.</summary>
        private string _outcome;

        /// <summary>
        ///     Fired when the event is created by the event factory, but before it is executed. Acts as a constructor mostly but
        ///     used in this way so that only the factory will call the method.
        /// </summary>
        public override void OnEventCreate()
        {
            base.OnEventCreate();

            _outcome = "A twitchy man shoves a graded slab in your face and demands cash.";
        }

        /// <summary>
        ///     Fired when the event handler associated with this enum type triggers action on target entity.
        /// </summary>
        /// <param name="eventExecutor">Entities which the event is going to directly affect.</param>
        public override void Execute(RandomEventInfo eventExecutor)
        {
            // Cast the source entity as vehicle; bail if it is not one so we never touch a null inventory.
            var vehicle = eventExecutor.SourceEntity as Entity.Vehicle.Vehicle;
            if (vehicle == null)
                return;

            var pitch = new StringBuilder();
            pitch.AppendLine("A sweaty man lunges out of a gas-station lot waving a slabbed card.");
            pitch.AppendLine("\"PSA 10, one-of-one, worth eight hundred easy. CASH ONLY, RIGHT NOW.\"");
            pitch.Append("You hand over the money and take the slab. Later, you get it checked...");

            var reveal = new StringBuilder();
            reveal.AppendLine(pitch.ToString());
            reveal.AppendLine();

            // One time in ten the slab is the real deal; the other nine it is a resealed fake.
            if (GameSimulationApp.Instance.Random.Next(10) == 0)
            {
                vehicle.Inventory[Entities.Cash].AddQuantity(RealCardPayout);
                reveal.Append(
                    $"It is REAL. The grade holds up and you flip it for ${RealCardPayout}. You got lucky.");
            }
            else
            {
                vehicle.Inventory[Entities.Cash].ReduceQuantity(FakeCardLoss);
                reveal.Append(
                    $"The slab is a resealed fake with a laser-printed label. You are out ${FakeCardLoss}.");
            }

            _outcome = reveal.ToString();
        }

        /// <summary>
        ///     Fired after the event is executed. Hands rendering off to the animated grifter reveal form instead of the plain
        ///     event text so the creep can leer and lunge while the player reads the outcome.
        /// </summary>
        /// <param name="eventExecutor">Form that executed the event from the random event window.</param>
        internal override bool OnPostExecute(EventExecutor eventExecutor)
        {
            base.OnPostExecute(eventExecutor);

            eventExecutor.SetForm(typeof(ScammerReveal));
            return true;
        }

        /// <summary>
        ///     Fired when the simulation would like to render the event. The animated reveal form reads this back out of
        ///     <see cref="RandomEventInfo.EventText" />, so it must contain the full pitch and outcome.
        /// </summary>
        /// <param name="userData">Source entity information for the event.</param>
        /// <returns>Text user interface string explaining the scam and its outcome.</returns>
        protected override string OnRender(RandomEventInfo userData)
        {
            return _outcome;
        }
    }
}
