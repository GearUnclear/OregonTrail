// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     A couple million people are marching to point out the country has no king, which a passer-by notes they have
    ///     been saying for 250 years. Traffic does not move. The party loses days but takes no damage.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class NoKingsMarch : LoseTime
    {
        /// <summary>
        ///     Grabs the correct number of days that should be skipped by the lose time event. The event skip day form that
        ///     follows will count down the number of days to zero before letting the player continue.
        /// </summary>
        /// <returns>Number of days that should be skipped in the simulation.</returns>
        protected override int DaysToSkip()
        {
            return 2;
        }

        /// <summary>
        ///     Defines the string that will be used to define the event and how it affects the user.
        /// </summary>
        /// <returns>
        ///     The reason days were skipped.<see cref="string" />.
        /// </returns>
        protected override string OnLostTimeReason()
        {
            return "A couple million folks are marching to say there is no king, like they have " +
                   "for 250 years. Nothing on the road moves.";
        }
    }
}
