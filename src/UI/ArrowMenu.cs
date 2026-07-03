using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OregonTrailDotNet.UI
{
    /// <summary>
    ///     A single selectable line in an <see cref="ArrowMenu" />. <see cref="Value" /> must exactly match whatever
    ///     string the owning screen's existing input parser already accepts for this choice (typically the enum's
    ///     underlying int, stringified) so arrow-selecting an option and typing its number remain equivalent.
    /// </summary>
    public sealed class ArrowMenuOption
    {
        public ArrowMenuOption(string text, string value, bool enabled = true)
        {
            Text = text;
            Value = value;
            Enabled = enabled;
        }

        public string Text { get; }

        public string Value { get; }

        public bool Enabled { get; }
    }

    /// <summary>
    ///     WolfCurses composes the whole screen as one flat string with no cursor-positioning or color API, so there is
    ///     no engine-level "highlighted list" primitive. This tracks a highlighted selection over a list of options and
    ///     renders it as plain text; Up/Down move the highlight, and Enter re-submits the highlighted option's
    ///     <see cref="ArrowMenuOption.Value" /> through the screen's existing text-input pipeline exactly as if the
    ///     player had typed it and pressed Enter, so no screen's input-parsing logic needs to change.
    /// </summary>
    public sealed class ArrowMenu
    {
        private List<ArrowMenuOption> _options = new List<ArrowMenuOption>();

        public int SelectedIndex { get; private set; }

        public bool HasOptions => _options.Count > 0;

        public string SelectedValue => HasOptions ? _options[SelectedIndex].Value : string.Empty;

        /// <summary>
        ///     Replaces the option list. If the previously-selected option (matched by <see cref="ArrowMenuOption.Value" />)
        ///     is still present, the highlight stays put across re-renders (e.g. after a purchase updates prices);
        ///     otherwise the highlight resets to the first enabled option.
        /// </summary>
        public void SetOptions(IEnumerable<ArrowMenuOption> options)
        {
            var previousValue = HasOptions ? _options[SelectedIndex].Value : null;
            _options = options.ToList();

            var matchIndex = previousValue == null ? -1 : _options.FindIndex(o => o.Value == previousValue);
            SelectedIndex = matchIndex >= 0 ? matchIndex : 0;

            if (!CurrentEnabled())
                MoveDown();
        }

        public void MoveUp()
        {
            Move(-1);
        }

        public void MoveDown()
        {
            Move(1);
        }

        private void Move(int direction)
        {
            if (!HasOptions)
                return;

            var start = SelectedIndex;
            do
            {
                SelectedIndex = (SelectedIndex + direction + _options.Count) % _options.Count;
            } while (!CurrentEnabled() && SelectedIndex != start);
        }

        private bool CurrentEnabled()
        {
            return HasOptions && _options[SelectedIndex].Enabled;
        }

        /// <summary>Renders one line per option, prefixing the highlighted line with a cursor marker.</summary>
        public string Render()
        {
            var builder = new StringBuilder();
            for (var i = 0; i < _options.Count; i++)
            {
                var option = _options[i];
                var marker = i == SelectedIndex ? ">" : " ";
                builder.AppendLine($"{marker} {option.Text}");
            }

            return builder.ToString();
        }
    }
}
