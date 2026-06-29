// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System.Diagnostics.CodeAnalysis;
using OregonTrailDotNet.Event.Prefab;
using OregonTrailDotNet.Module.Director;

namespace OregonTrailDotNet.Event.Wild
{
    /// <summary>
    ///     An MLM "hun" who mortgaged the house for ten crates of leggings corners the party to buy in. There is no way to
    ///     leave the pitch quickly; breaking away from the recruitment costs the party time on the trail.
    /// </summary>
    [DirectorEvent(EventCategory.Wild)]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public sealed class MlmHun : LoseTime
    {
        /// <summary>
        ///     Grabs the correct number of days that should be skipped by the lose time event. The event skip day form that
        ///     follows will count down the number of days to zero before letting the player continue.
        /// </summary>
        /// <returns>Number of days that should be skipped in the simulation.</returns>
        protected override int DaysToSkip()
        {
            return 1;
        }

        /// <summary>
        ///     Defines the string that will be used to define the event and how it affects the user.
        /// </summary>
        /// <returns>
        ///     The reason days were skipped.<see cref="string" />.
        /// </returns>
        protected override string OnLostTimeReason()
        {
            return "An MLM hun who mortgaged the house for ten crates of leggings needs you to buy in.";
        }
    }
}
