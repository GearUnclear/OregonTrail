// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com) 
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Core;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Travel.Store.Help
{
    /// <summary>
    ///     Offers up some free information about what items are important to the player and what they mean for the during the
    ///     course of the simulation. Reskinned as the Buc-ee's greeter walking the family through the travel-center aisles.
    /// </summary>
    [ParentWindow(typeof(Travel))]
    public sealed class StoreWelcome : Form<TravelInfo>
    {
        /// <summary>
        ///     Keeps track if the player has read all the advice and this dialog needs to be closed.
        /// </summary>
        private bool _hasReadAdvice;

        /// <summary>
        ///     Keeps track of the message we want to show to the player but only build it actually once.
        /// </summary>
        private StringBuilder _storeHelp;

        /// <summary>
        ///     Determines which panel of information we have shown to the user, pressing return will cycle through them.
        /// </summary>
        private int _adviceCount;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StoreWelcome" /> class.
        ///     Offers up some free information about what items are important to the player and what they mean for the during the
        ///     course of the simulation.
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public StoreWelcome(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>Default is FALSE. Setting to TRUE allows characters and input buffer to be read when submitted.</remarks>
        public override bool InputFillsBuffer => false;

        /// <summary>
        ///     Fired after the state has been completely attached to the simulation letting the state know it can browse the user
        ///     data and other properties below it.
        /// </summary>
        public override void OnFormPostCreate()
        {
            base.OnFormPostCreate();

            _hasReadAdvice = false;
            _storeHelp = new StringBuilder();
            UpdateAdvice();
        }

        /// <summary>
        ///     Since the advice can change we have to do this in chunks.
        /// </summary>
        private void UpdateAdvice()
        {
            // Clear any previous string builder message.
            _storeHelp.Clear();

            // Create the current state of our advice to player.
            _storeHelp.Append($"{Environment.NewLine}Welcome to Buc-ee's. So you're{Environment.NewLine}");
            _storeHelp.Append($"driving to Seattle! We can fix{Environment.NewLine}");
            _storeHelp.Append($"you up with what you need:{Environment.NewLine}{Environment.NewLine}");

            if (_adviceCount <= 0)
            {
                _storeHelp.Append($" - five-gallon gas cans to{Environment.NewLine}");
                _storeHelp.Append($" keep the car rolling{Environment.NewLine}{Environment.NewLine}");

                _storeHelp.Append($" - crates of MLM leggings,{Environment.NewLine}");
                _storeHelp.Append($" still in the bag{Environment.NewLine}{Environment.NewLine}");
            }
            else if (_adviceCount == 1)
            {
                _storeHelp.Append($" - plenty of snacks for the{Environment.NewLine}");
                _storeHelp.Append($" drive{Environment.NewLine}{Environment.NewLine}");

                _storeHelp.Append($" - boxes of ammo, right{Environment.NewLine}");
                _storeHelp.Append($" there by the flour{Environment.NewLine}{Environment.NewLine}");

                _storeHelp.Append($" - spare parts for your{Environment.NewLine}");
                _storeHelp.Append($" car{Environment.NewLine}{Environment.NewLine}");
            }

            // Wait for user input...
            _storeHelp.Append(InputManager.PRESSENTER);
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
            return _storeHelp.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // On the last advice panel we flip a normal boolean to know we are definitely done here.
            if (_hasReadAdvice)
            {
                ClearForm();
                return;
            }

            // Tick the advice to next panel when we get input.
            if (_adviceCount <= 0)
            {
                _adviceCount++;
                UpdateAdvice();
                return;
            }

            // Make sure we don't run final logic to show actual store until we show all advice to player.
            if (_adviceCount < 1)
                return;

            _hasReadAdvice = true;
            SetForm(typeof(Store));
        }
    }
}