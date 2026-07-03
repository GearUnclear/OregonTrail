// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Renderer;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.GameOver
{
    /// <summary>
    ///     Attached when the party leader dies, or the vehicle reaches the end of the trail.
    /// </summary>
    [ParentWindow(typeof(GameOver))]
    public sealed class GameWin : InputForm<GameOverInfo>
    {
        /// <summary>
        ///     Holds reference to end game text that will be shown to the user.
        /// </summary>
        private readonly StringBuilder _gameOver;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameWin" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public GameWin(IWindow window) : base(window)
        {
            _gameOver = new StringBuilder();
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The dialog prompt text.<see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            _gameOver.AppendLine(SceneArt.SmilingSun);
            _gameOver.AppendLine("Congratulations! You have made it to Seattle!");
            _gameOver.AppendLine("Sunshine State Mutual regrets to inform you");
            _gameOver.AppendLine($"your new ZIP code is also under review.{Environment.NewLine}");
            _gameOver.AppendLine("Let's tally your final Net Worth and Clout score.");
            return _gameOver.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            SetForm(typeof(FinalPoints));
        }
    }
}