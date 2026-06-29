// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Person
{
    /// <summary>
    ///     Struck by a car while walking the shoulder of a 55-mph US highway. There was no sidewalk; there is rarely a
    ///     sidewalk. The driver did not stop.
    /// </summary>
    [DirectorEvent(EventCategory.Person)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class Concussion : PersonInjure
    {
        /// <summary>Fired after the event has executed and the injury flag set on the person.</summary>
        /// <param name="person">Person whom is now injured by whatever you say they are here.</param>
        /// <returns>Describes what type of physical injury has come to the person.</returns>
        protected override string OnPostInjury(Entity.Person.Person person)
        {
            return $"{person.Name} was struck by a car walking the shoulder of a 55-mph highway. There was no sidewalk.";
        }
    }
}