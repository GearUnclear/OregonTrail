// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Person
{
    /// <summary>
    ///     Heat exhaustion under a heat dome. It was, the locals are quick to point out, a dry heat: 110 degrees for the
    ///     thirty-first straight day.
    /// </summary>
    [DirectorEvent(EventCategory.Person)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class TyphoidFever : PersonInfect
    {
        /// <summary>Fired after the event has executed and the infection flag set on the person.</summary>
        /// <param name="person">Person whom is now infected by whatever you say they are here.</param>
        /// <returns>Name or type of infection the person is currently affected with.</returns>
        protected override string OnPostInfection(Entity.Person.Person person)
        {
            return $"{person.Name} has heat exhaustion. It was a dry heat: 110°F for the 31st straight day.";
        }
    }
}