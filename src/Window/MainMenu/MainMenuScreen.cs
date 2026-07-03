// Created for arrow-key navigation support.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OregonTrailDotNet.UI;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.MainMenu
{
    /// <summary>
    ///     The main menu's 5-item command list, rendered as an arrow-navigable Form since WolfCurses' bare-window
    ///     AddCommand menu has no hook to highlight (see <see cref="MainMenu.OnFormChange" />).
    /// </summary>
    [ParentWindow(typeof(MainMenu))]
    public sealed class MainMenuScreen : Form<NewGameInfo>
    {
        private readonly ArrowMenu _menu = new ArrowMenu();

        private Dictionary<string, Action> _handlersByValue = new Dictionary<string, Action>();

        public MainMenuScreen(IWindow window) : base(window)
        {
        }

        public override string OnRenderForm()
        {
            var mainMenu = (MainMenu) ParentWindow;
            var commands = mainMenu.GetMenuCommands().ToList();

            _handlersByValue = commands.ToDictionary(c => ((int) c.Command).ToString(), c => c.Handler);
            _menu.SetOptions(commands.Select(c =>
                new ArrowMenuOption($"{(int) c.Command}. {c.Command.ToDescriptionAttribute()}",
                    ((int) c.Command).ToString())));

            GameSimulationApp.Instance.ActiveMenu = _menu;

            var text = new StringBuilder();
            text.Append(MainMenu.BuildMenuHeader());
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
