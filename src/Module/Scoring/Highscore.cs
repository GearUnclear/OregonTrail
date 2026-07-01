// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM


using WolfCurses.Utility;

namespace OregonTrailDotNet.Module.Scoring
{
    /// <summary>
    ///     Defines an object that keeps track of a particular high score of a given simulation round. This includes the name
    ///     of the person for bragging rights, points they earned in total at the end of the trip, and the overall rating this
    ///     game them.
    /// </summary>
    public sealed class Highscore
    {
        /// <summary>
        ///     Internal enumeration value for the score the player actually had as enumeration value, we convert this to string
        ///     when asked for it.
        /// </summary>
        private readonly Performance _rating;

        /// <summary>Initializes a new instance of the <see cref="T:OregonTrailDotNet.Module.Scoring.Highscore" /> class.</summary>
        /// <param name="name">The name.</param>
        /// <param name="points">The points.</param>
        public Highscore(string name, int points)
        {
            // PassengerLeader of party and total number of points.
            Name = name;
            Points = points;

            // Rank the players performance based on the number of points they have.
            // End-game score is dominated by the surviving-party health term (4 people * 500) times the
            // leader's profession multiplier, so the realistic ceilings are ~3,700 (Banker x1), ~5,000
            // (Carpenter x2) and ~6,900 (Farmer x3). The old 7,000 TrailGuide gate was unreachable by ANY
            // profession (dead content); 6,000 lets an optimally-played Farmer earn the top rating while
            // keeping it out of reach for Carpenter/Banker, preserving the profession prestige ladder.
            if (points >= 6000)
                _rating = Performance.TrailGuide;
            else if ((points >= 3000) && (points < 6000))
                _rating = Performance.Adventurer;
            else if (points < 3000)
                _rating = Performance.Greenhorn;
        }

        /// <summary>
        ///     Names of the leader of the vehicle party.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Total number of points the player accumulated.
        /// </summary>
        public int Points { get; }

        /// <summary>
        ///     Stores an enumeration as read only inside high score object, returns a string for the rating using extension method
        ///     to get description attribute so it looks correct when rendered and shown to users.
        /// </summary>
        public string Rating => _rating.ToDescriptionAttribute();
    }
}