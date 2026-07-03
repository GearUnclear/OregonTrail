// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using OregonTrailDotNet.Entity;
using OregonTrailDotNet.Event;
using OregonTrailDotNet.Window.RandomEvent;

namespace OregonTrailDotNet.Module.Director
{
    /// <summary>
    ///     Numbers events and allows them to propagate through it and to other parts of the simulation. Lives inside of the
    ///     game simulation normally.
    /// </summary>
    public sealed class EventDirectorModule : WolfCurses.Module.Module
    {
        /// <summary>
        ///     Fired when an event has been triggered by the director.
        /// </summary>
        public delegate void EventTriggered(IEntity simEntity, EventProduct directorEvent);

        /// <summary>
        ///     Creates event items on behalf of the director when he rolls the dice looking for one to trigger.
        /// </summary>
        private EventFactory _eventFactory;

        /// <summary>
        ///     Most recently fired random events, oldest first. Used to bias the factory away from immediate
        ///     repeats so variety reads through even once events start firing often.
        /// </summary>
        private readonly List<Type> _recentEvents = new List<Type>();

        /// <summary>
        ///     How many recently fired events to remember and avoid repeating on the next rolls.
        /// </summary>
        private const int RecentEventMemory = 3;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EventDirectorModule" /> class.
        ///     Initializes a new instance of the <see cref="T:TrailSimulation.Core.ModuleProduct" /> class.
        /// </summary>
        public EventDirectorModule()
        {
            // Creates a new event factory, and event history list. 
            _eventFactory = new EventFactory();
        }

        /// <summary>
        ///     Fired when the simulation is closing and needs to clear out any data structures that it created so the program can
        ///     exit cleanly.
        /// </summary>
        public override void Destroy()
        {
            _eventFactory = null;
        }

        /// <summary>
        ///     Fired when an event has been triggered by the director.
        /// </summary>
        public event EventTriggered OnEventTriggered;

        /// <summary>
        ///     Gathers all of the events by specified type and then rolls the virtual dice to determine if any of the events in
        ///     the enumeration should trigger.
        /// </summary>
        /// <param name="sourceEntity">Entities which will be affected by event if triggered.</param>
        /// <param name="eventCategory">Event type the dice will be rolled against and attempted to trigger.</param>
        public void TriggerEventByType(IEntity sourceEntity, EventCategory eventCategory)
        {
            // Roll a category-weighted chance instead of the old flat 1% gate. The base game hardcoded
            // every category to 1%, which meant the 40+ authored events almost never showed up and play
            // felt empty and repetitive. See CategoryChance for the per-category odds and why they differ.
            if (GameSimulationApp.Instance.Random.Next(100) >= CategoryChance(eventCategory))
                return;

            // Create a random event by type enumeration; the factory picks one for us, biased away from the
            // handful most recently fired so the player doesn't see the same incident back-to-back.
            var randomEventProduct = _eventFactory.CreateRandomByType(eventCategory, _recentEvents);

            // Check to make sure the event returned actually exists.
            if (randomEventProduct == null)
                return;

            // Remember it so the next few rolls steer away from repeating it.
            RememberEvent(randomEventProduct.GetType());

            // Invokes the event which will give it full control over simulation.
            ExecuteEvent(sourceEntity, randomEventProduct);
        }

        /// <summary>
        ///     Percent chance (0-100) per roll that a category fires when <see cref="TriggerEventByType" /> is
        ///     called. The base game used a flat 1% for everything; these are weighted so flavorful, low-stakes
        ///     categories surface often while lethal ones stay rare. Crucially, Person is rolled once PER
        ///     passenger every travel day, so its odds compound — keep it low or the party gets sick constantly.
        /// </summary>
        /// <param name="eventCategory">Category about to be rolled.</param>
        /// <returns>Percent chance the roll should pass.</returns>
        private static int CategoryChance(EventCategory eventCategory)
        {
            switch (eventCategory)
            {
                case EventCategory.Wild:
                    // Roadside America (strangers, crowds, roadside stands): mostly flavor and the main
                    // driver of "something is always happening", so this fires the most.
                    return 8;
                case EventCategory.Animal:
                    // Stampedes, snakebites, feral hogs: moderate stakes, fires fairly often.
                    return 5;
                case EventCategory.Vehicle:
                    // Breakdowns and flats: recoverable and part of the fun, rolled once per day.
                    return 4;
                case EventCategory.Weather:
                    // Fog, hail, heat: rolled once per day while moving.
                    return 3;
                case EventCategory.Person:
                    // Illness and injury, rolled once per passenger per day -> ~8%/day for a full party.
                    // Kept deliberately low so the journey is eventful without being a death spiral.
                    return 2;
                case EventCategory.RiverCross:
                    // A river crossing is already a discrete, tense moment; leave it at the original odds.
                    return 1;
                default:
                    return 1;
            }
        }

        /// <summary>
        ///     Records a freshly fired event type and trims the memory to the most recent
        ///     <see cref="RecentEventMemory" /> entries so the factory can avoid immediate repeats.
        /// </summary>
        /// <param name="eventType">The concrete event type that just fired.</param>
        private void RememberEvent(Type eventType)
        {
            _recentEvents.Add(eventType);
            while (_recentEvents.Count > RecentEventMemory)
                _recentEvents.RemoveAt(0);
        }

        /// <summary>
        ///     Triggers an event directly by type of reference. Event must have [EventDirector] attribute to be
        ///     registered in the factory correctly.
        /// </summary>
        /// <param name="sourceEntity">Entities which will be affected by event if triggered.</param>
        /// <param name="eventType">System type that represents the type of event to trigger.</param>
        public void TriggerEvent(IEntity sourceEntity, Type eventType)
        {
            // Grab the event item from the factory that makes them.
            var eventProduct = _eventFactory.CreateInstance(eventType);
            ExecuteEvent(sourceEntity, eventProduct);
        }

        /// <summary>
        ///     Primary worker for the event factory, pulled into it's own method here so all the trigger event types can call it.
        ///     This will attach the random event game Windows and then fire an event to trigger the event execution in that
        ///     Windows
        ///     then it will be able to display any relevant data about what happened.
        /// </summary>
        /// <param name="sourceEntity">Entities which will be affected by event if triggered.</param>
        /// <param name="directorEvent">Created instance of event that will be executed on simulation in random game Windows.</param>
        private void ExecuteEvent(IEntity sourceEntity, EventProduct directorEvent)
        {
            // Attach random event game Windows before triggering event since it will listen for it using event delegate.
            GameSimulationApp.Instance.WindowManager.Add(typeof(RandomEvent));

            // Fire off event so primary game simulation knows we executed an event with an event.
            OnEventTriggered?.Invoke(sourceEntity, directorEvent);
        }
    }
}