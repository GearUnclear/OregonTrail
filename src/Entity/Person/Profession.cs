// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using WolfCurses.Utility;

namespace OregonTrailDotNet.Entity.Person
{
    /// <summary>
    ///     The profession.
    /// </summary>
    public enum Profession
    {
        /// <summary>
        ///     The crypto bro (reskin of the original banker; identifier and value preserved for scoring/hunting math).
        /// </summary>
        [Description("Be a crypto bro from Miami Beach")] Banker = 1,

        /// <summary>
        ///     The DoorDash driver (reskin of the original carpenter; identifier and value preserved for scoring/hunting math).
        /// </summary>
        [Description("Be a DoorDash driver from Ohio")] Carpenter = 2,

        /// <summary>
        ///     The faith-walk streamer (reskin of the original farmer; identifier and value preserved for scoring/hunting math).
        /// </summary>
        [Description("Be a faith-walk streamer from Illinois")] Farmer = 3
    }
}