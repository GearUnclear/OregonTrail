// Created for arrow-key navigation support.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OregonTrailDotNet.UI;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel
{
    /// <summary>
    ///     The base travel menu ("Keep driving", "Check supplies", ...). WolfCurses renders a bare Window's
    ///     AddCommand menu itself with no override hook, so this Form stands in for that bare-window state
    ///     whenever nothing else has the form slot (see <see cref="Travel.OnFormChange" />), letting it render an
    ///     arrow-navigable list instead.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class TravelMenu : Form<TravelInfo>
    {
        private readonly ArrowMenu _menu = new ArrowMenu();

        private Dictionary<string, Action> _handlersByValue = new Dictionary<string, Action>();

        public TravelMenu(IWindow window) : base(window)
        {
        }

        public override string OnRenderForm()
        {
            var travel = (Travel) ParentWindow;
            var commands = travel.GetMenuCommands().ToList();

            // The on-screen number is the option's 1-based position in the list, NOT the enum's underlying
            // integer value. The set of available commands (and therefore which enum values appear) changes
            // with location status, so keying the visible number to the enum value rendered the menu out of
            // order and with gaps (e.g. "1-7, 10, 9, 11"). Numbering by position keeps the list a clean
            // ascending 1, 2, 3, ... in every location state, and the value we hand each option matches its
            // displayed number so arrow-selecting and typing the number stay equivalent.
            _handlersByValue = new Dictionary<string, Action>();
            var options = new List<ArrowMenuOption>();
            for (var i = 0; i < commands.Count; i++)
            {
                var displayNumber = (i + 1).ToString();
                _handlersByValue[displayNumber] = commands[i].Handler;
                options.Add(new ArrowMenuOption(
                    $"{displayNumber}. {commands[i].Command.ToDescriptionAttribute()}", displayNumber));
            }

            _menu.SetOptions(options);

            GameSimulationApp.Instance.ActiveMenu = _menu;

            var text = new StringBuilder();
            text.Append(Travel.BuildMenuHeader());
            text.Append(_menu.Render());
            return text.ToString();
        }

        public override void OnInputBufferReturned(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            if (_handlersByValue.TryGetValue(input.Trim(), out var handler))
                handler();
        }
    }
}
