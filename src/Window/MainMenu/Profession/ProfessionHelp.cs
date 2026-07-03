// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Window;
using WolfCurses.Window.Form;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.Window.MainMenu.Profession
{
    /// <summary>
    ///     Shows information about what the player leader professions mean and how it affects the party, vehicle, game
    ///     difficulty, and scoring at the end (if they make it).
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class ProfessionHelp : InputForm<NewGameInfo>
    {
        /// <summary>Initializes a new instance of the <see cref="ProfessionHelp" /> class.</summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public ProfessionHelp(IWindow window) : base(window)
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
            // Information about professions, the life each one represents, and how that life scores.
            var job = new StringBuilder();
            job.AppendLine($"{Environment.NewLine}Three kinds of people are fleeing");
            job.AppendLine("to Seattle this year. Each starts");
            job.AppendLine($"the drive with a different life:{Environment.NewLine}");

            job.AppendLine("The CRYPTO BRO from Miami Beach has");
            job.AppendLine("liquidity, a ring light, and a wallet");
            job.AppendLine("seed phrase tattooed where his mother");
            job.AppendLine("can't see it. He starts richest and so");
            job.AppendLine($"has the easiest road -- and the least glory.{Environment.NewLine}");

            job.AppendLine("The DOORDASH DRIVER from Ohio knows every");
            job.AppendLine("gas station between here and the coast by");
            job.AppendLine("its bathroom. Middling funds, iron nerves,");
            job.AppendLine($"a tolerance for traffic bordering on holy.{Environment.NewLine}");

            job.AppendLine("The FAITH-WALK STREAMER from Illinois has");
            job.AppendLine("followers, forty dollars, and the serene");
            job.AppendLine("confidence of someone livestreaming their");
            job.AppendLine("own pilgrimage. Poorest start, hardest");
            job.AppendLine($"road -- and the greatest points if you live.{Environment.NewLine}");

            job.AppendLine("The harder the life, the bigger the legend.");
            job.AppendLine($"History is graded on a curve.{Environment.NewLine}");
            return job.ToString();
        }

        /// <summary>
        ///     Fired when the dialog receives favorable input and determines a response based on this. From this method it is
        ///     common to attach another state, or remove the current state based on the response.
        /// </summary>
        /// <param name="reponse">The response the dialog parsed from simulation input buffer.</param>
        protected override void OnDialogResponse(DialogResponse reponse)
        {
            // parentGameMode.State = new ProfessionSelector(parentGameMode, UserData);
            SetForm(typeof(ProfessionSelector));
        }
    }
}