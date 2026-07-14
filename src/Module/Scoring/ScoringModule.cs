// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Collections.Generic;
using System.Linq;

namespace OregonTrailDotNet.Module.Scoring
{
    /// <summary>
    ///     Keeps track of all the high scores, loads them from a default set that can always be reset to. If there are no
    ///     custom scores to be loaded then the defaults will be used, the high-score should not be reset when the simulation
    ///     is reset instead only when manually reset from he manager module for it which can be accessed by the main menu
    ///     under options.
    /// </summary>
    public sealed class ScoringModule : WolfCurses.Module.Module
    {
        /// <summary>
        ///     Keeps track of the total number of points the player has earned through the course of the game.
        /// </summary>
        private List<Highscore> _highScores;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ScoringModule" /> class.
        ///     Scoring tracker and tabulator for end game results from current simulation state.
        /// </summary>
        public ScoringModule()
        {
            Reset();
        }

        /// <summary>
        ///     Keeps track of the total number of points the player has earned through the course of the game.
        /// </summary>
        public IEnumerable<Highscore> TopTen
        {
            get { return _highScores.OrderByDescending(x => x.Points).Take(10); }
        }

        /// <summary>
        ///     Original high scores from Apple II version of the game.
        /// </summary>
        public static IEnumerable<Highscore> DefaultTopTen => new List<Highscore>
        {
            new Highscore("Brayden Kessler", 7650),
            new Highscore("Ashleigh Vandermeer", 5694),
            new Highscore("Pastor Dax Holloway", 4138),
            new Highscore("Tanner Whitlock", 2945),
            new Highscore("Kayleigh Brubaker", 2052),
            new Highscore("Hunter Delacroix", 1401),
            new Highscore("Braylynn Ostrander", 937),
            new Highscore("Colton Reinhardt", 615),
            new Highscore("Madisyn Tran-Buckley", 396),
            new Highscore("Chip Vandergrift", 250)
        };

        /// <summary>Adds a new high-score to the list.</summary>
        /// <param name="score"></param>
        public void Add(Highscore score)
        {
            _highScores.Add(score);
        }

        /// <summary>
        ///     Fired when the simulation is closing and needs to clear out any data structures that it created so the program can
        ///     exit cleanly.
        /// </summary>
        public override void Destroy()
        {
            base.Destroy();

            // TODO: Save the high score list as JSON before it is destroyed.

            // Destroyed the high score list.
            _highScores = null;
        }

        /// <summary>
        ///     Makes the top ten list reset to the original top ten hard-coded defaults.
        /// </summary>
        public void Reset()
        {
            _highScores = new List<Highscore>(DefaultTopTen);

            // TODO: Load custom list from JSON with user high scores altered from defaults.
        }
    }
}