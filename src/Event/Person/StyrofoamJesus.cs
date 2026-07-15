// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Person
{
    /// <summary>
    ///     Incinerated when the 62-ft Styrofoam "Touchdown Jesus" was struck by lightning and burned, despite the lightning
    ///     rods. Fired manually only, at the Touchdown Jesus shrine (see Travel.ArriveAtLocation); the injury effect itself is
    ///     the unchanged PersonInjure mechanic.
    /// </summary>
    [DirectorEvent(EventCategory.Person, EventExecution.ManualOnly)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class StyrofoamJesus : PersonInjure
    {
        /// <summary>Fired after the event has executed and the injury flag set on the person.</summary>
        /// <param name="person">Person whom is now injured by whatever you say they are here.</param>
        /// <returns>Describes what type of physical injury has come to the person.</returns>
        protected override string OnPostInjury(Entity.Person.Person person)
        {
            return $"{person.Name} was caught beneath the 62-ft Styrofoam Jesus when " +
                   "lightning struck it and it went up in flames. The lightning rods, a " +
                   "passer-by noted, did not help.";
        }
    }
}
