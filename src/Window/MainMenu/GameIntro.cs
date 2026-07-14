// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using OregonTrailDotNet.Renderer;
using OregonTrailDotNet.Window.MainMenu.Profession;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.MainMenu
{
    /// <summary>
    ///     Lengthy, satirical preamble shown at the very start of the new-game flow (before the player picks a
    ///     profession). It establishes the basis of the 2028 American Roadtrip: why an uninsurable family is fleeing
    ///     Cape Coral for Seattle, what they are leaving behind, and what the country between the two coasts has become.
    ///     The text is paged -- each ENTER advances one screen -- so the wall of lore stays readable on an 80x24
    ///     console instead of scrolling off the top. The final page hands off to <see cref="ProfessionSelector" />.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class GameIntro : Form<NewGameInfo>
    {
        /// <summary>
        ///     The preamble, stored as one array of lines per page. WolfCurses renders a form's body verbatim -- only
        ///     its table helper word-wraps -- so these line breaks are deliberate, not decorative: they keep each line
        ///     inside the 80-column console the game targets. Lines are reflowed to an even ~58-character measure with
        ///     no single-word orphans so the column reads cleanly even on a wide terminal. An empty string is a blank
        ///     line between paragraphs.
        /// </summary>
        private static readonly string[][] PageLines =
        {
            // 1. The basis: the year, the world, and the form letter that started it all.
            new[]
            {
                "The year is 2028.",
                "",
                "The map of America has not changed, but the list of",
                "places you may legally live has. The insurers went",
                "first -- quietly, by ZIP code -- and where insurers go",
                "the banks follow, and where banks go, so goes anyone",
                "who can still read a spreadsheet.",
                "",
                "Your house in Cape Coral, Florida was ruled",
                "\"uninsurable at any premium\" in a form letter with a",
                "smiling cartoon sun in the letterhead. The HOA held an",
                "emergency meeting about it and voted to repaint the",
                "clubhouse.",
                "",
                "So you are leaving for Seattle."
            },

            // 2. The player's life: who you are and what you are leaving behind.
            new[]
            {
                "You are not pioneers. You are a family with a paid-off",
                "car, a folder of important documents, and a group chat",
                "full of relatives who think you are overreacting.",
                "",
                "Behind you: a screened lanai, a thirty-year mortgage on",
                "a house now worth less than the U-Haul deposit, a job",
                "that became \"fully remote, indefinitely,\" and a favorite",
                "diner that is currently, in the legal sense, a reef.",
                "",
                "What fits in the car goes with you. What doesn't gets",
                "left for the tide. A neighbor's kid already gave you",
                "forty dollars and a firm handshake for the patio set.",
                "",
                "The children believe this is a vacation. You have",
                "elected not to correct them."
            },

            // 3. The country in between: roadside America, as it actually is.
            new[]
            {
                "Between you and the Pacific lies the country as it",
                "really is in 2028: gloriously, defiantly itself.",
                "",
                "You will refuel at a Buc-ee's the size of a regional",
                "airport. You will photograph a sixty-foot fiberglass",
                "Jesus with both arms raised. You will ford an Interstate",
                "where a river used to be merely a suggestion. You will",
                "be waved through some towns and waved AT in others.",
                "",
                "There are no covered wagons out here. There are toll",
                "lanes, open-carry greeters, sovereign citizens with",
                "laminated paperwork, and a life-size cow sculpted from",
                "butter and kept on ice. Treat each one as you would a",
                "weather event."
            },

            // 4. The destination and the stakes.
            new[]
            {
                "Seattle is not paradise. It rains with real conviction",
                "and an oat-milk latte costs what a tank of gas used to.",
                "But it is insurable, it is cool enough to bear, and it",
                "is uphill from the sea.",
                "",
                "That is the entire pitch.",
                "",
                "The road will test your car, your snack reserve, and",
                "the patience of everyone strapped into it. Keep fuel in",
                "the tank, food in the cooler, and at least one",
                "functioning adult at the wheel.",
                "",
                "Not everyone survives this drive. Those who do not get a",
                "shoulder, a guardrail, and a single sentence that you",
                "get to write for them."
            },

            // 5. Send-off into profession selection.
            new[]
            {
                "One last thing before you back out of the driveway.",
                "",
                "Who you were before all this still matters -- not to the",
                "road, which could not care less, but to your wallet,",
                "which cares enormously. A crypto bro from Miami Beach",
                "sets out with a fat account and a soft handshake. A",
                "faith-walk streamer sets out with followers, forty",
                "dollars, and the sheer nerve of the righteous.",
                "",
                "The softer your start, the smaller the legend. The",
                "harder your start, the bigger the points.",
                "",
                "History is graded on a curve. Choose your life."
            }
        };

        /// <summary>
        ///     Index of the page currently being shown. Advanced one step per ENTER until the preamble is exhausted.
        /// </summary>
        private int _page;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GameIntro" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public GameIntro(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>Default is FALSE. Setting to TRUE allows characters and input buffer to be read when submitted.</remarks>
        public override bool InputFillsBuffer => true;

        /// <summary>
        ///     Returns a text only representation of the current game Windows state. Could be a statement, information, question
        ///     waiting input, etc.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string OnRenderForm()
        {
            var intro = new StringBuilder();

            // Beat 1: opening sun page (before any prose).
            if (_page == 0)
            {
                intro.AppendLine(SceneArt.SmilingSun);
                intro.Append("Press ENTER to continue...");
                return intro.ToString();
            }

            // Beat 2: closing sun page (after the last prose page).
            if (_page == PageLines.Length + 1)
            {
                intro.AppendLine(SceneArt.SmilingSun);
                intro.Append("Press ENTER to choose your life...");
                return intro.ToString();
            }

            // Prose pages: _page is 1-based relative to PageLines (1 .. PageLines.Length).
            int prosePage = _page;
            intro.AppendLine($"{Environment.NewLine}THE ASPHALT TRAIL -- 2028 AMERICAN ROADTRIP");
            intro.AppendLine($"(page {prosePage} of {PageLines.Length})");
            intro.AppendLine(string.Join(Environment.NewLine, PageLines[prosePage - 1]));
            intro.AppendLine(string.Empty);
            intro.Append("Press ENTER to continue...");
            return intro.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // Any ENTER turns the page; whatever the player typed is ignored, the road does not care.
            _page++;

            // Total flow: [SUN(0)] -> prose(1..N) -> [SUN(N+1)] -> ProfessionSelector.
            // Advance to profession selection once both sun pages and all prose have been seen.
            if (_page >= PageLines.Length + 2)
                SetForm(typeof(ProfessionSelector));
        }
    }
}
