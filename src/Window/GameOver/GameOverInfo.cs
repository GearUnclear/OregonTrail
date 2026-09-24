// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System.Collections.Generic;
using OregonTrailDotNet.Module.Scoring;
using WolfCurses.Window;

namespace OregonTrailDotNet.Window.GameOver
{
    /// <summary>
    ///     Represents all of the custom logic used by the game over window to process the end of the game and show an fail
    ///     state or point tabulation system.
    /// </summary>
    public sealed class GameOverInfo : WindowData
    {
        public Highscore FinalScore { get; set; }
        public int BasePoints { get; set; }
        public int ChoiceScoreDelta { get; set; }
        public int Multiplier { get; set; }
        public IReadOnlyList<ScoreLine> ScoreLines { get; set; }
        public IReadOnlyList<string> Epilogue { get; set; }
    }

    public sealed record ScoreLine(int Quantity, string Description, int Points);
}
