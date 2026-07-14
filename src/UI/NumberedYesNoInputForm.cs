using WolfCurses.Window;
using WolfCurses.Window.Form.Input;

namespace OregonTrailDotNet.UI
{
    /// <summary>
    ///     Shared base for every two-option Yes/No confirmation screen that renders an <see cref="ArrowMenu" />
    ///     labelled "1. Yes" / "2. No". The WolfCurses base <see cref="InputForm{T}.OnInputBufferReturned" />
    ///     only understands the letter/word forms (Y/YES/TRUE and N/NO/FALSE) -- there is no numeric branch -- so
    ///     typing the very numbers the menu tells you to type used to fall through to the Custom/No branch and do
    ///     the wrong thing. This intercepts the raw input buffer and remaps a typed "1" to "Y" and "2" to "N"
    ///     before handing off to the base parser, so the displayed numbers finally do what they say.
    ///
    ///     Everything else passes straight through untouched: typing "Y"/"N"/"yes"/"no" still works, and the
    ///     arrow-menu's own selected value ("y"/"n", injected on a bare Enter by Program.cs) is unaffected. This
    ///     only remaps the exact strings "1" and "2", so real multi-option numbered menus are never derived from
    ///     this type and remain unaffected.
    /// </summary>
    /// <typeparam name="T">The parent window's <see cref="WindowData" /> type, matching the parent Window.</typeparam>
    public abstract class NumberedYesNoInputForm<T> : InputForm<T> where T : WindowData, new()
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="NumberedYesNoInputForm{T}" /> class.
        /// </summary>
        /// <param name="window">The window.</param>
        protected NumberedYesNoInputForm(IWindow window) : base(window)
        {
        }

        /// <summary>
        ///     Intercepts the raw input buffer before the base <see cref="InputForm{T}" /> parses it, remapping the
        ///     "1" and "2" the numbered Yes/No menu shows into the "Y"/"N" the base parser understands. Every other
        ///     input is forwarded unchanged.
        /// </summary>
        /// <param name="input">Contents of the input buffer submitted by the player.</param>
        public override void OnInputBufferReturned(string input)
        {
            var normalized = input?.Trim();
            if (normalized == "1")
                input = "Y";
            else if (normalized == "2")
                input = "N";

            base.OnInputBufferReturned(input);
        }
    }
}
