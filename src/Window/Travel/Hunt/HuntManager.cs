using System;
using WolfCurses;

namespace OregonTrailDotNet.Window.Travel.Hunt
{
    /// <summary>A continuous timing game. Each tray accepts one attempt; outcomes never open a dialog.</summary>
    public sealed class HuntManager : ITick
    {
        public const int MAXFOOD = 100;
        public const int TrayCount = 8;
        public const int PassMilliseconds = 1800;
        private const int InputGraceMilliseconds = 750;
        private readonly Func<long> _clock;
        private readonly Random _random;
        private long _started;
        private long _nextTray;
        public int Round { get; private set; }
        public int ZoneStart { get; private set; }
        public int ZoneEnd { get; private set; }
        public int TrayPounds { get; private set; }
        public string TrayName { get; private set; }
        public int KillWeight { get; private set; }
        public int Grabs { get; private set; }
        public string Feedback { get; private set; } = "Watch the marker. Grab inside the highlighted zone.";
        public bool Resolved { get; private set; }
        public bool ShouldEndHunt { get; private set; }
        public string HuntInfo => $"Food sweep — tray {Round}/{TrayCount}. {Feedback} Haul: {KillWeight} lb.";

        public HuntManager() : this(new Random(), () => Environment.TickCount64) { }
        public HuntManager(Random random, Func<long> clock)
        {
            _random = random;
            _clock = clock;
            NextTray();
        }

        private void NextTray()
        {
            Round++;
            TrayPounds = _random.Next(10, 26);
            var names = new[] { "Sandwich platter", "Bakery box", "Fruit tray", "Pasta pan", "Snack crate" };
            TrayName = names[_random.Next(names.Length)];
            ZoneStart = _random.Next(500, 1251);
            ZoneEnd = ZoneStart + (TrayPounds >= 20 ? 180 : 260);
            _started = _clock();
            Resolved = false;
        }

        // The browser reports the marker time at the input event, avoiding network latency in scoring.
        // Bound it against the server clock; round-specific actions and Resolved prevent replay/double awards.
        public bool TryGrab(int elapsed)
        {
            var age = _clock() - _started;
            if (Resolved || ShouldEndHunt || elapsed < 0 || elapsed > PassMilliseconds ||
                elapsed > age + 100 || age - elapsed > InputGraceMilliseconds) return false;
            var hit = elapsed >= ZoneStart && elapsed <= ZoneEnd;
            var gained = Math.Min(TrayPounds, MAXFOOD - KillWeight);
            if (hit)
            {
                KillWeight += gained;
                Grabs++;
            }
            Feedback = hit ? $"Got it! +{gained} lb." : elapsed < ZoneStart
                ? "Too early! The tray slipped past." : "Too late! Another shopper got it.";
            Resolve();
            return true;
        }

        private void Resolve()
        {
            Resolved = true;
            _nextTray = _clock() + 350;
        }

        public void OnTick(bool systemTick, bool skipDay)
        {
            if (skipDay || ShouldEndHunt) return;
            if (!Resolved && _clock() - _started >= PassMilliseconds + InputGraceMilliseconds)
            {
                Feedback = "Missed it! Another shopper took the tray.";
                Resolve();
            }
            if (!Resolved || _clock() < _nextTray) return;
            if (Round >= TrayCount || KillWeight >= MAXFOOD) ShouldEndHunt = true;
            else NextTray();
        }
    }
}
