// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.MainMenu.Help
{
    /// <summary>
    ///     Third and final panel on clout information, explains how the player's profession selection affects final scoring as
    ///     a multiplier since starting as a crypto bro is a handicap.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class PointsMultiplyerHelp : InputForm<NewGameInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PointsMultiplyerHelp" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public PointsMultiplyerHelp(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            var pointsProfession = new StringBuilder();
            pointsProfession.Append(
                $"{Environment.NewLine}On Arriving in Seattle{Environment.NewLine}{Environment.NewLine}");
            pointsProfession.AppendLine("You receive a clout multiplier");
            pointsProfession.AppendLine("for your hustle. Because the");
            pointsProfession.AppendLine("less you start with the more");
            pointsProfession.AppendLine("you have to prove, you receive");
            pointsProfession.AppendLine("double clout upon arriving in");
            pointsProfession.AppendLine("Seattle as a DoorDash driver,");
            pointsProfession.AppendLine("and triple clout for arriving");
            pointsProfession.AppendLine($"as a faith-walk streamer.{Environment.NewLine}");
            return pointsProfession.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            ClearForm();
        }
    }
}