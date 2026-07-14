// Created by Ron 'Maxwolf' McDowell (ron.mcdowell@gmail.com)
// Timestamp 01/03/2016@1:50 AM

using System;
using System.Text;
using WolfCurses.Utility;
using WolfCurses.Window;
using WolfCurses.Window.Form;

namespace OregonTrailDotNet.Window.Graveyard
{
    /// <summary>
    ///     Allows for the message on the Tombstone to be edited or added, either way this window will get the job done.
    ///     Will limit the input of the epitaph, do basic whitespace checks and trimming, and -- because the underlying
    ///     input layer only feeds letters and digits into the buffer (spaces and punctuation are silently dropped by
    ///     WolfCurses' InputManager.AddCharToInputBuffer, which we do not own) -- rebuild a spaced sentence by having the
    ///     player commit one WORD per ENTER press. This is the only path that can preserve the spaces in the GoFundMe
    ///     epitaph the intro promises the player they get to write.
    /// </summary>
    [ParentWindow(typeof(Graveyard))]
    public sealed class EpitaphEditor : Form<TombstoneInfo>
    {
        /// <summary>
        ///     Defines how long an epitaph on a tombstone can be in characters, spaces included. Sized to hold a full
        ///     one-sentence GoFundMe eulogy (e.g. "Died fleeing an uninsurable state killed by fog and MLM snacks")
        ///     while still fitting on a single rendered line -- the epitaph prints un-wrapped below the headstone art,
        ///     and the console/web terminal is 80 columns, so 64 leaves comfortable margin and never overflows the art.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private const int EPITAPH_MAXLENGTH = 64;

        /// <summary>
        ///     Accumulates the finished epitaph across submissions. The low-level input buffer cannot hold a space, so
        ///     each ENTER commits the current word into this builder and we re-insert the spaces between words here.
        /// </summary>
        private readonly StringBuilder _epitaph;

        /// <summary>
        ///     String builder that will hold representation of the tombstone for the player to see.
        /// </summary>
        private readonly StringBuilder _epitaphPrompt;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EpitaphEditor" /> class.
        ///     This constructor will be used by the other one
        /// </summary>
        /// <param name="window">The window.</param>
        // ReSharper disable once UnusedMember.Global
        public EpitaphEditor(IWindow window) : base(window)
        {
            _epitaph = new StringBuilder();
            _epitaphPrompt = new StringBuilder();
        }

        /// <summary>
        ///     Determines if user input is currently allowed to be typed and filled into the input buffer.
        /// </summary>
        /// <remarks>
        ///     Always TRUE so every ENTER -- including the blank one that finishes the epitaph -- is routed to
        ///     <see cref="OnInputBufferReturned" /> as free text rather than being parsed as a menu command. Length is
        ///     enforced when each word is committed, not by refusing keystrokes.
        /// </remarks>
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
            // Show the headstone, the epitaph built so far, and how to enter a spaced sentence one word at a time.
            _epitaphPrompt.Clear();
            _epitaphPrompt.AppendLine($"{Environment.NewLine}{UserData.Tombstone}");
            _epitaphPrompt.AppendLine("Write their GoFundMe epitaph one WORD at a time.");
            _epitaphPrompt.AppendLine("Press ENTER after each word (a space is added for you).");
            _epitaphPrompt.AppendLine("Press ENTER on an empty line when the eulogy is done.");
            _epitaphPrompt.AppendLine(string.Empty);
            _epitaphPrompt.AppendLine(
                $"Epitaph so far: {(_epitaph.Length > 0 ? _epitaph.ToString() : "(nothing yet)")}");
            return _epitaphPrompt.ToString();
        }

        /// <summary>Fired when the game Windows current state is not null and input buffer does not match any known command.</summary>
        /// <param name="input">Contents of the input buffer which didn't match any known command in parent game Windows.</param>
        public override void OnInputBufferReturned(string input)
        {
            // The buffer only ever contains letters/digits (the input layer strips spaces and punctuation), so each
            // submission is a single word. A blank ENTER signals the player is finished with the eulogy.
            var word = input.Trim();
            if (word.Length <= 0)
            {
                // Lock in whatever has been written, trimmed and clamped so it never overruns the headstone.
                UserData.Tombstone.Epitaph = _epitaph.ToString().Trim().Truncate(EPITAPH_MAXLENGTH);

                // Confirm with the player this is what they wanted the tombstone to say.
                SetForm(typeof(EpitaphConfirm));
                return;
            }

            // Re-insert the space the input layer swallowed, then append the committed word and clamp to the limit so
            // an over-long word can never push the epitaph past what a single tombstone line can hold.
            if (_epitaph.Length > 0)
                _epitaph.Append(' ');
            _epitaph.Append(word);
            if (_epitaph.Length > EPITAPH_MAXLENGTH)
                _epitaph.Length = EPITAPH_MAXLENGTH;

            // Stay on this form so the player can keep adding words until they submit a blank line.
        }
    }
}
