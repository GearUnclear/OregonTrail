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

            _handlersByValue = commands.ToDictionary(c => ((int) c.Command).ToString(), c => c.Handler);
            _menu.SetOptions(commands.Select(c =>
                new ArrowMenuOption($"{(int) c.Command}. {c.Command.ToDescriptionAttribute()}",
                    ((int) c.Command).ToString())));

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
