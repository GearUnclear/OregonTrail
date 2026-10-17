// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.MainMenu.Names
{
    /// <summary>
    ///     Gets the name of a player for a particular index in the player name user data object. This will also offer the user
    ///     a chance to confirm their selection in another state, reset if they don't like it, and also generate a random user
    ///     name if they just press enter at the prompt for a name.
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class InputPlayerNames : Form<NewGameInfo>
    {
        /// <summary>
        ///     References the string that makes up the question about player names and also showing previous ones that have been
        ///     entered for continuity sake.
        /// </summary>
        private StringBuilder _inputNamesHelp;

        /// <summary>Explains why the last typed name was refused, shown until a usable name is entered.</summary>
        private string _rejection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InputPlayerNames" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public InputPlayerNames(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            // Pass the game data to the simulation for each new game Windows state.
            GameSimulationApp.Instance.SetStartInfo(UserData);

            // Create string builder so we only build up this data once.
            _inputNamesHelp = new StringBuilder();

            // Add the question text from constructor parameter.
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (UserData.PlayerNameIndex)
            {
                case 0:
                    _inputNamesHelp.Append(Environment.NewLine +
                                           $"{MainMenu.LEADER_QUESTION}");
                    break;
                case 1:
                    _inputNamesHelp.Append(Environment.NewLine +
                                           $"{MainMenu.MembersQuestion}" +
                                           $"{Environment.NewLine}{Environment.NewLine}");
                    break;
                case 2:
                    _inputNamesHelp.Append(Environment.NewLine +
                                           $"{MainMenu.MembersQuestion}" +
                                           $"{Environment.NewLine}{Environment.NewLine}");
                    break;
                case 3:
                    _inputNamesHelp.Append(Environment.NewLine +
                                           $"{MainMenu.MembersQuestion}" +
                                           $"{Environment.NewLine}{Environment.NewLine}");
                    break;
            }

            // Only print player names if we have some to actually print.
            if (UserData.PlayerNames.Count > 0)
            {
                // Loop through all the player names and get their current state.
                var crewNumber = 1;

                // Loop through every player and print their name.
                for (var index = 0; index < GameSimulationApp.Instance.Vehicle.MaxPartySize; index++)
                {
                    var name = string.Empty;
                    if (index < UserData.PlayerNames.Count)
                        name = UserData.PlayerNames[index];

                    // First name in list is always the leader.
                    var isLeader = (UserData.PlayerNames.IndexOf(name) == 0) && (crewNumber == 1);
                    _inputNamesHelp.AppendFormat(isLeader
                        ? $"  {crewNumber} - {name} (leader){Environment.NewLine}"
                        : $"  {crewNumber} - {name}{Environment.NewLine}");
                    crewNumber++;
                }

                // Wait for user input...
                _inputNamesHelp.Append("\n(Enter names or press Enter)");
            }
        }

        /// <summary>
        ///     Returns a text only representation of the current game Windows state. Could be a statement, information, question
        ///     waiting input, etc.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public override string OnRenderForm()
        {
            return _rejection == null
                ? _inputNamesHelp.ToString()
                : $"{_inputNamesHelp}{Environment.NewLine}{_rejection}";
        }

        /// <summary>
        ///     Returns why a typed name cannot join the party, or null when it can. Blank input is always allowed because it
        ///     generates the remaining names.
        /// </summary>
        internal string RejectionFor(string input)
        {
            return RejectionFor(UserData.PlayerNames.Take(UserData.PlayerNameIndex), input);
        }

        /// <summary>
        ///     Returns why a typed name cannot join a party already holding <paramref name="existingNames" />, or null when it
        ///     can. Blank input is allowed; text with no letters or digits would be stored as an empty name, so it is refused.
        /// </summary>
        internal static string RejectionFor(IEnumerable<string> existingNames, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;
            if (NameKey(input).Length == 0)
                return "Names need at least one letter or number.";

            var match = MatchingName(existingNames, input);
            return match == null ? null : $"{match.Trim()} is already in the party. Choose a different name.";
        }

        /// <summary>
        ///     Reduces a name to the letters and digits the console input buffer keeps, so "Mary Ann", "maryann" and
        ///     "Mary-Ann" compare the same way they would be stored.
        /// </summary>
        internal static string NameKey(string name)
        {
            return new string((name ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
        }

        /// <summary>True when a non-blank name matches an existing one, ignoring case, whitespace and punctuation.</summary>
        internal static bool IsNameTaken(IEnumerable<string> existingNames, string candidate)
        {
            return MatchingName(existingNames, candidate) != null;
        }

        private static string MatchingName(IEnumerable<string> existingNames, string candidate)
        {
            var key = NameKey(candidate);
            return key.Length == 0
                ? null
                : existingNames.FirstOrDefault(name => string.Equals(NameKey(name), key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // If player enters empty name fill out all the slots with random ones.
            if (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            {
                // Only fill out names for slots that are empty.
                var random = new Random(GameSimulationApp.Instance.Random.Next());
                for (var i = 0; i < GameSimulationApp.Instance.Vehicle.MaxPartySize - UserData.PlayerNameIndex; i++)
                    UserData.PlayerNames.Insert(UserData.PlayerNameIndex, GetPlayerName(UserData.PlayerNames, random));

                // Attach state to confirm randomized name selection, skipping manual entry with the return.
                SetForm(typeof(ConfirmPlayerNames));
                return;
            }

            // Refuse a name already in the party so event and death messages stay unambiguous, and one with nothing to store.
            var rejection = RejectionFor(input);
            if (rejection != null)
            {
                _rejection = rejection;
                return;
            }

            // Add the name to list since we will have something at this point even if randomly generated.
            UserData.PlayerNames.Insert(UserData.PlayerNameIndex, input.Trim());
            UserData.PlayerNameIndex++;

            // Change the state to either confirm or input the next name based on index of name we are entering.
            SetForm(UserData.PlayerNameIndex < GameSimulationApp.Instance.Vehicle.MaxPartySize
                ? typeof(InputPlayerNames)
                : typeof(ConfirmPlayerNames));
        }

        /// <summary>
        ///     Returns a random name if there is an empty name returned, we assume the player doesn't care and just give him one.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        internal static string GetPlayerName(IEnumerable<string> existingNames, Random random)
        {
            string[] names =
            {
                "Bob", "Joe", "Sally", "Tim", "Steve", "Zeke", "Suzan", "Rebekah", "Young", "Marquitta",
                "Kristy", "Sharice", "Joanna", "Chrystal", "Genevie", "Angela", "Ruthann", "Viva", "Iris", "Anderson",
                "Siobhan", "Karey", "Jolie", "Carlene", "Lekisha", "Buck"
            };
            var used = new HashSet<string>(existingNames.Select(NameKey), StringComparer.OrdinalIgnoreCase);
            var available = names.Where(name => !used.Contains(NameKey(name))).ToArray();
            if (available.Length > 0)
                return available[random.Next(available.Length)];

            // Keep generation bounded even if a future party size exhausts the catalog.
            var suffix = 1;
            while (used.Contains(NameKey($"Traveler {suffix}"))) suffix++;
            return $"Traveler {suffix}";
        }
    }
}
