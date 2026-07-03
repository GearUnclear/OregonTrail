// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/01/2016@7:40 PM

using System;
using System.Threading;

namespace OregonTrailDotNet
{
    /// <summary>
    ///     Trail Simulation Game - Console Edition
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///     Example console app for game simulation entry point.
        /// </summary>
        public static int Main()
        {
            // Create console with title, no cursor, make CTRL-C act as input.
            Console.Title = "Oregon Trail Clone";
            Console.WriteLine("Starting...");
            Console.CursorVisible = false;
            Console.CancelKeyPress += Console_CancelKeyPress;

            // Because I want things to look correct like progress bars.
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            // Create game simulation singleton instance, and start it.
            GameSimulationApp.Create();

            // Hook event to know when screen buffer wants to redraw the entire console screen.
            GameSimulationApp.Instance.SceneGraph.ScreenBufferDirtyEvent += Simulation_ScreenBufferDirtyEvent;

            // Prevent console session from closing.
            while (GameSimulationApp.Instance != null)
            {
                // Simulation takes any numbers of pulses to determine seconds elapsed.
                GameSimulationApp.Instance.OnTick(true);

                // Check if a key is being pressed, without blocking thread.
                if (Console.KeyAvailable)
                {
                    // GetModule the key that was pressed, without printing it to console.
                    var key = Console.ReadKey(true);

                    // If enter is pressed, pass whatever we have to simulation.
                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (key.Key)
                    {
                        case ConsoleKey.Enter:
                            // If the player hasn't typed anything and the active screen has an arrow menu,
                            // submit the highlighted option exactly as if its number had been typed - this
                            // keeps every screen's existing input parsing untouched.
                            var activeMenu = GameSimulationApp.Instance.ActiveMenu;
                            if (string.IsNullOrEmpty(GameSimulationApp.Instance.InputManager.InputBuffer))
                            {
                                // On a bare Enter, inject the highlighted option's value so it flows through the
                                // screen's existing text-input parser exactly as if the player had typed it.
                                if (activeMenu?.HasOptions ?? false)
                                {
                                    foreach (var selectedChar in activeMenu.SelectedValue)
                                        GameSimulationApp.Instance.InputManager.AddCharToInputBuffer(selectedChar);
                                }
                            }

                            GameSimulationApp.Instance.InputManager.SendInputBufferAsCommand();
                            break;
                        case ConsoleKey.Backspace:
                            GameSimulationApp.Instance.InputManager.RemoveLastCharOfInputBuffer();
                            break;
                        case ConsoleKey.UpArrow:
                            GameSimulationApp.Instance.ActiveMenu?.MoveUp();
                            break;
                        case ConsoleKey.DownArrow:
                            GameSimulationApp.Instance.ActiveMenu?.MoveDown();
                            break;
                        case ConsoleKey.LeftArrow:
                            GameSimulationApp.Instance.OnLeftPressed?.Invoke();
                            break;
                        case ConsoleKey.RightArrow:
                            GameSimulationApp.Instance.OnRightPressed?.Invoke();
                            break;
                        default:
                            GameSimulationApp.Instance.InputManager.AddCharToInputBuffer(key.KeyChar);
                            break;
                    }
                }

                // Do not consume all of the CPU, allow other messages to occur.
                Thread.Sleep(1);
            }

            // Make user press any key to close out the simulation completely, this way they know it closed without error.
            Console.Clear();
            Console.WriteLine("Goodbye!");
            Console.WriteLine("Press ANY KEY to close this window...");
            Console.ReadKey();
            return 0;
        }

        /// <summary>Write all text from objects to screen.</summary>
        /// <param name="tuiContent">The text user interface content.</param>
        private static void Simulation_ScreenBufferDirtyEvent(string tuiContent)
        {
            var tuiContentSplit = tuiContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (var index = 0; index < Console.WindowHeight - 1; index++)
            {
                Console.SetCursorPosition(0, index);

                var emptyStringData = new string(' ', Console.WindowWidth);

                if (tuiContentSplit.Length > index)
                {
                    emptyStringData = tuiContentSplit[index].PadRight(Console.WindowWidth);
                }

                Console.Write(emptyStringData);
            }
        }

        /// <summary>
        ///     Fired when the user presses CTRL-C on their keyboard, this is only relevant to operating system tick and this view
        ///     of simulation. If moved into another framework like game engine this statement would be removed and just destroy
        ///     the simulation when the engine is destroyed using its overrides.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // Destroy the simulation.
            GameSimulationApp.Instance.Destroy();

            // Stop the operating system from killing the entire process.
            e.Cancel = true;
        }
    }
}