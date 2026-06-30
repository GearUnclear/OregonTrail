namespace OregonTrailDotNet.Renderer
{
    /// <summary>
    ///     A looping multi-frame ASCII animation. Mirrors how WolfCurses' MarqueeBar is used: keep one in a Form,
    ///     call <see cref="Step" /> each simulation tick, and render <see cref="CurrentFrame" />. Used for fixed
    ///     frame loops such as the river water shimmer.
    /// </summary>
    public sealed class AsciiAnimation
    {
        private readonly string[] _frames;
        private int _index;

        /// <summary>Initializes the animation with its ordered frames (must contain at least one).</summary>
        /// <param name="frames">Frames cycled in order, wrapping back to the first.</param>
        public AsciiAnimation(params string[] frames)
        {
            _frames = (frames == null) || (frames.Length == 0) ? new[] { string.Empty } : frames;
        }

        /// <summary>The frame currently showing.</summary>
        public string CurrentFrame => _frames[_index];

        /// <summary>Advances to the next frame (wrapping) and returns it.</summary>
        public string Step()
        {
            _index = (_index + 1) % _frames.Length;
            return _frames[_index];
        }
    }
}
