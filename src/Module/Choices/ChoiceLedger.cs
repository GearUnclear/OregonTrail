namespace OregonTrailDotNet.Module.Choices
{
    /// <summary>
    ///     Per-game ledger that records the forking decisions the player made along the trail. Each decision contributes an
    ///     optional score delta (applied to the final endgame tabulation) and an optional epilogue line (shown on the game
    ///     over screen). Rebuilt every game via <see cref="GameSimulationApp.Restart" /> and torn down on destroy.
    /// </summary>
    public sealed class ChoiceLedger
    {
        private readonly System.Collections.Generic.List<string> _epilogue = new();
        private readonly System.Collections.Generic.Dictionary<string, string> _decisions = new();
        public int ScoreDelta { get; private set; }
        public System.Collections.Generic.IReadOnlyList<string> Epilogue => _epilogue;
        public bool HasDecision(string key) => _decisions.ContainsKey(key);
        public string GetDecision(string key) => _decisions.TryGetValue(key, out var v) ? v : null;

        public void Record(string decisionKey, string optionKey, int scoreDelta, string epilogueLine)
        {
            _decisions[decisionKey] = optionKey;
            ScoreDelta += scoreDelta;
            if (!string.IsNullOrWhiteSpace(epilogueLine)) _epilogue.Add(epilogueLine);
        }
    }
}
