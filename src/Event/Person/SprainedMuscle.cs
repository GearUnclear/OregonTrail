// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Person
{
    /// <summary>
    ///     A minor, survivable injury followed by a $9,076 surprise ambulance bill. The ride was eight blocks and out of
    ///     network. The injury heals; the invoice does not.
    /// </summary>
    [DirectorEvent(EventCategory.Person)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class SprainedMuscle : PersonInjure
    {
        /// <summary>Fired after the event has executed and the injury flag set on the person.</summary>
        /// <param name="person">Person whom is now injured by whatever you say they are here.</param>
        /// <returns>Describes what type of physical injury has come to the person.</returns>
        protected override string OnPostInjury(Entity.Person.Person person)
        {
            return $"{person.Name} has a survivable injury and a $9,076 surprise ambulance bill.";
        }
    }
}