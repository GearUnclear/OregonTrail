// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Renderer;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.Travel.Hunt.Help
{
    /// <summary>
    ///     Prompt that proceeds the food sweep when accessed from the travel menu. Explains to the player how the controls
    ///     work and what is expected of them in regards to how the game mode operates.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class HuntingPrompt : InputForm<TravelInfo>
    {
        /// <summary>
        ///     References the message we show to the user that explains how the food sweep works.
        /// </summary>
        private readonly StringBuilder _huntHelp;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InputForm{T}" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public HuntingPrompt(IWindow window) : base(window)
        {
            _huntHelp = new StringBuilder();
        }

        /// <summary>
        ///     Fired when dialog prompt is attached to active game Windows and would like to have a string returned.
        /// </summary>
        /// <returns>
        ///     The dialog prompt text.<see cref="string" />.
        /// </returns>
        protected override string OnDialogPrompt()
        {
            // Clear out previous food sweep help messages.
            _huntHelp.Clear();

            // Food-counter scene above the instructions.
            _huntHelp.AppendLine(SceneArt.FoodSweep);

            // Create the prompt for explaining how the food sweep works.
            _huntHelp.AppendLine($"{Environment.NewLine}Food Sweep Instructions{Environment.NewLine}");

            // Explain how timer works, how grabbing works and food weight limits.
            const string huntTextTop =
                "The food sweep has a timer which represents how long until the doors close. When the timer reaches zero the sweep is over. " +
                "You can only haul 100 pounds of food back to the SUV, don't grab more than you keep since you just burn ammo holding your spot.";

            // Explain how grabbing works, how player has limited window of opportunity to snag the tray.
            const string huntTextBottom =
                "When a tray hits the table you have until another shopper grabs it to type the grab word shown. " +
                "If you don't type fast enough you risk fumbling the grab and burning ammo on nothing!";

            // Add the top and bottom food sweep text on their own lines.
            _huntHelp.AppendLine(huntTextTop.WordWrap());
            _huntHelp.AppendLine(huntTextBottom.WordWrap());

            // Returns the now processed hunting help prompt to renderer.
            return _huntHelp.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // Creates a new food sweep with trays for the player to grab.
            UserData.GenerateHunt();

            // Attaches the form that lets us manipulate and view this data.
            SetForm(typeof(Hunting));
        }
    }
}