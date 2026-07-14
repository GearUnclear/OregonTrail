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
    ///     Shows basic information about how the game works, how traveling works, rules for winning and losing.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class RulesHelp : InputForm<NewGameInfo>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RulesHelp" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public RulesHelp(IWindow window) : base(window)
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
            var aboutTrail = new StringBuilder();
            aboutTrail.AppendLine(
                $"{Environment.NewLine}Your road trip up the Asphalt Trail starts in Cape Coral,");
            aboutTrail.AppendLine("Florida. You're driving your family to Seattle, Washington --");
            aboutTrail.AppendLine(
                $"about {GameSimulationApp.Instance.Trail.Length:N0} miles of open road.{Environment.NewLine}");

            aboutTrail.AppendLine("The car is paid off. Before you leave, stock up on supplies.");
            aboutTrail.AppendLine($"Here's what each one is for:{Environment.NewLine}");

            aboutTrail.AppendLine(" * Gas - fuel for the car. More gas means more miles");
            aboutTrail.AppendLine($"   between fill-ups, so you reach Seattle faster.{Environment.NewLine}");

            aboutTrail.AppendLine(" * Snacks - food for your family. Run low and everyone's");
            aboutTrail.AppendLine($"   health drops, so always keep some on hand.{Environment.NewLine}");

            aboutTrail.AppendLine(" * Ammo - used to grab extra food during sweeps along the way and");
            aboutTrail.AppendLine($"   to handle trouble on the road.{Environment.NewLine}");

            aboutTrail.AppendLine(" * Leggings - your barter goods. River guides take these");
            aboutTrail.AppendLine($"   as payment when a flooded crossing blocks the road.{Environment.NewLine}");

            aboutTrail.AppendLine(" * Other supplies - first-aid kits, tools, and spare parts");
            aboutTrail.AppendLine($"   (tires, alternators, transmissions) to fix breakdowns.{Environment.NewLine}");
            return aboutTrail.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // parentGameMode.State = null;
            ClearForm();
        }
    }
}