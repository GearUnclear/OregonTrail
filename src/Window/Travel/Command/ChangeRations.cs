// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Collections.Generic;
using System.Text;
using OregonTrailDotNet.Entity.Person;
using OregonTrailDotNet.UI;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Command
{
    /// <summary>
    ///     Allows the player to change the amount of food their party members will have access to in a given day, the purpose
    ///     of which is to limit the amount they take in to slow the loss of food per pound. This has many affects on the
    ///     simulation such as disease, chance for breaking body parts, and or complete death from starvation.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class ChangeRations : Form<TravelInfo>
    {
        /// <summary>
        ///     Builds up the ration information and selection text.
        /// </summary>
        private StringBuilder _ration;

        /// <summary>
        ///     Tracks the arrow-key highlighted line among the ration choices.
        /// </summary>
        private readonly ArrowMenu _menu = new ArrowMenu();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChangeRations" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public ChangeRations(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            _ration = new StringBuilder();
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
            // Rebuilt every render pass (not just on form creation) so the arrow-key highlight stays current.
            _ration.Clear();
            _ration.AppendLine($"{Environment.NewLine}Change food rations");
            _ration.AppendLine("(currently");
            _ration.AppendLine(
                $"\"{GameSimulationApp.Instance.Vehicle.Ration.ToDescriptionAttribute()}\"){Environment.NewLine}");
            _ration.AppendLine("The amount of food the people in");
            _ration.AppendLine("your family eat each day can");
            _ration.AppendLine($"change. These amounts are:{Environment.NewLine}");

            var options = new List<ArrowMenuOption>
            {
                new ArrowMenuOption("1. filling - meals are large and generous.", "1"),
                new ArrowMenuOption("2. meager - meals are small, but adequate.", "2"),
                new ArrowMenuOption("3. bare bones - meals are very small, everyone stays hungry.", "3")
            };

            _menu.SetOptions(options);
            GameSimulationApp.Instance.ActiveMenu = _menu;
            _ration.Append(_menu.Render());

            return _ration.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            switch (input.ToUpperInvariant())
            {
                case "1":
                    GameSimulationApp.Instance.Vehicle.ChangeRations(RationLevel.Filling);
                    ClearForm();
                    break;
                case "2":
                    GameSimulationApp.Instance.Vehicle.ChangeRations(RationLevel.Meager);
                    ClearForm();
                    break;
                case "3":
                    GameSimulationApp.Instance.Vehicle.ChangeRations(RationLevel.BareBones);
                    ClearForm();
                    break;
                default:
                    SetForm(typeof(ChangeRations));
                    break;
            }
        }
    }
}