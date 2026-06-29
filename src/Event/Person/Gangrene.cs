// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Person
{
    /// <summary>
    ///     A rattlesnake bite, the snake coiled on a "Don't Tread on Me" flag at a roadside stand. The tissue around the wound
    ///     is dying and the antivenom is, predictably, out of network.
    /// </summary>
    [DirectorEvent(EventCategory.Person)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class Gangrene : PersonInfect
    {
        /// <summary>Fired after the event has executed and the infection flag set on the person.</summary>
        /// <param name="person">Person whom is now infected by whatever you say they are here.</param>
        /// <returns>Name or type of infection the person is currently affected with.</returns>
        protected override string OnPostInfection(Entity.Person.Person person)
        {
            return $"{person.Name} was bitten by a \"Don't Tread on Me\" rattlesnake.";
        }
    }
}