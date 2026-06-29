// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using WolfCurses.Utility;

namespace OregonTrailDotNet.Module.Scoring
{
    /// <summary>
    ///     Defines a rank on the Net Worth and Clout Leaderboard the player can earn based on the number of points they
    ///     receive during the entire course of the road trip. At the end after tabulation this enum is assigned as an
    ///     overall representation of the clout level.
    /// </summary>
    public enum Performance
    {
        /// <summary>
        ///     Easy
        /// </summary>
        [Description("Tourist")] Greenhorn = 1,

        /// <summary>
        ///     Medium
        /// </summary>
        [Description("Influencer")] Adventurer = 2,

        /// <summary>
        ///     Hard
        /// </summary>
        [Description("Verified")] TrailGuide = 3
    }
}