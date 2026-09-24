using System.Reflection;

namespace OregonTrailDotNet.Web;

/// <summary>Runs the existing rules inside one journey. Only its JourneyWorker may call this adapter.</summary>
internal sealed class GameEngine : IGameEngine
{
    private BrowserGame _game;
    private int _settlingPulses;
    public GameSnapshotDto Snapshot { get; private set; }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        GameSimulationApp.Activate(null);
        GameSimulationApp.Create();
        _game = new BrowserGame(GameSimulationApp.Instance);
        try
        {
            for (var attempt = 0; attempt < 60; attempt++)
            {
                PulseOnce();
                if (_game.Simulation.ActiveMenu?.HasOptions == true)
                {
                    CapturePresentation(false);
                    return;
                }
                await Task.Delay(50, cancellationToken);
            }
            throw new InvalidOperationException("The journey did not initialize.");
        }
        finally { Deactivate(); }
    }

    public GameSnapshotDto Tick()
    {
        Activate();
        try
        {
            var seconds = _game.Simulation?.FixedTicks;
            PulseOnce();
            _game.Simulation = GameSimulationApp.Instance;
            // System pulses settle queued input. Only a fixed game tick or a recent action needs projection.
            if (_settlingPulses > 0 || seconds != _game.Simulation?.FixedTicks)
            {
                if (_settlingPulses > 0) _settlingPulses--;
                CapturePresentation(false);
            }
            return Snapshot;
        }
        finally { Deactivate(); }
    }

    public async Task<GameActionResponseDto> DispatchAsync(GameActionRequest request, CancellationToken cancellationToken)
    {
        Activate();
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ActionId))
                return Rejected("invalid-action", "An actionId is required.", Snapshot);
            if (request.ExpectedRevision != _game.Revision)
                return Rejected("stale-revision", "The game moved to a newer state. Review it and try again.", Snapshot);
            if (!_game.Bindings.TryGetValue(request.ActionId, out var binding))
                return Rejected("invalid-action", "That action is not available on the current screen.", Snapshot);
            if (!ValidateRequest(binding, request, out var input, out var message))
                return Rejected("invalid-value", message, Snapshot);

            if (binding.Kind == PresentationActionKind.Crypto)
            {
                if (_game.CurrentForm is not Window.Travel.Crypto.CryptoDesk desk || !desk.Execute(binding.LegacyValue, input))
                    return Rejected("invalid-value", binding.LegacyValue == "rename"
                        ? "Use 1–24 letters, numbers, spaces or hyphens, including at least one letter or number."
                        : "That exchange action is no longer available.", Snapshot);
            }
            else if (binding.Kind == PresentationActionKind.Creator)
            {
                if (_game.CurrentForm is not Window.Travel.Creator.CreatorDesk creator || !creator.Execute(binding.LegacyValue, input))
                    return Rejected("invalid-value", binding.LegacyValue == "start"
                        ? "Use a channel name of 1–32 letters, numbers, spaces or simple punctuation."
                        : "That studio action is no longer available.", Snapshot);
            }
            else await ExecuteAsync(_game, binding, input, cancellationToken);
            // Create/restart may set the execution-local instance in the async helper's context.
            Activate();
            PulseOnce();
            _game.Simulation = GameSimulationApp.Instance;
            _settlingPulses = 3;
            CapturePresentation(true);
            return new GameActionResponseDto(true, null, null, Snapshot);
        }
        finally { Deactivate(); }
    }

    private void CapturePresentation(bool forceRevision)
    {
        var frame = PresentationCatalog.Build(_game.Simulation, _game.Revision);
        if (!forceRevision && string.Equals(_game.SurfaceKey, frame.SurfaceKey, StringComparison.Ordinal)) return;
        _game.Revision++;
        _game.SurfaceKey = frame.SurfaceKey;
        _game.Bindings = frame.Bindings;
        _game.CurrentForm = frame.CurrentForm;
        Snapshot = frame.Snapshot with { Revision = _game.Revision };
    }

    private void Activate() => GameSimulationApp.Activate(_game.Simulation);
    private void Deactivate()
    {
        _game.Simulation = GameSimulationApp.Instance;
        GameSimulationApp.Activate(null);
    }

    public void Dispose()
    {
        if (_game == null) return;
        Activate();
        try { _game.Simulation?.Destroy(); }
        finally { Deactivate(); }
    }

    private Task ExecuteAsync(BrowserGame game, PresentationActionBinding binding, string input, CancellationToken cancellationToken)
    {
        switch (binding.Kind)
        {
            case PresentationActionKind.Submit:
                SendLegacyInput(input ?? binding.LegacyValue ?? string.Empty);
                break;
            case PresentationActionKind.Select:
                SelectMenuIndex(binding.MenuIndex);
                SendLegacyInput(binding.LegacyValue ?? string.Empty);
                break;
            case PresentationActionKind.AdjustLeft:
                GameSimulationApp.Instance?.OnLeftPressed?.Invoke();
                break;
            case PresentationActionKind.AdjustRight:
                GameSimulationApp.Instance?.OnRightPressed?.Invoke();
                break;
            case PresentationActionKind.StoreAdjust:
                InvokeStore(game, "AdjustQuantity", binding.StoreItem, binding.Delta);
                break;
            case PresentationActionKind.StoreSet:
                InvokeStore(game, "SetQuantity", binding.StoreItem, int.Parse(input));
                break;
            case PresentationActionKind.SkipIntro:
                AdvanceIntro(game);
                break;
            case PresentationActionKind.PrepareDeparture:
                return AdvanceToPackingAsync(game, cancellationToken);
            case PresentationActionKind.Restart:
                if (GameSimulationApp.Instance == null)
                    RestartEndedGame(game);
                else
                    GameSimulationApp.Instance.Restart();
                break;
            case PresentationActionKind.CreatorOpen:
                if (game.CurrentForm is Window.Travel.Command.ContinueOnTrail driving) driving.OpenCreator();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(binding.Kind), binding.Kind, "Unknown action kind.");
        }
        return Task.CompletedTask;
    }

    private static void RestartEndedGame(BrowserGame game)
    {
        GameSimulationApp.Create();
        game.Simulation = GameSimulationApp.Instance;
    }

    private void AdvanceIntro(BrowserGame game)
    {
        // The console spreads this story over seven pages to fit an 80x24 screen. The browser presents the full
        // opening at once, so one semantic action advances those page turns before showing the real first choice.
        for (var page = 0; page < 16; page++)
        {
            if (PresentationCatalog.Build(game.Simulation, game.Revision).CurrentForm is not
                Window.MainMenu.GameIntro)
                return;

            SendLegacyInput(string.Empty);
            PulseOnce();
        }

        throw new InvalidOperationException("The journey introduction did not advance to background selection.");
    }

    private async Task AdvanceToPackingAsync(BrowserGame game, CancellationToken cancellationToken)
    {
        // Skip the console shopping guidance, stopping at the packing choice before the supply store.
        for (var step = 0; step < 12; step++)
        {
            var form = PresentationCatalog.Build(game.Simulation, game.Revision).CurrentForm;
            if (form is Window.Travel.Decision.PackTheCarDecision)
                return;

            if (form?.GetType().Name is "InitialItemsHelp" or "StoreHelp")
                SendLegacyInput(string.Empty);
            else if (form != null)
                throw new InvalidOperationException("The initial store guidance changed unexpectedly.");

            PulseOnce();
            await Task.Delay(50, cancellationToken);
        }

        throw new InvalidOperationException("The departure packing choice did not open.");
    }

    private static bool ValidateRequest(
        PresentationActionBinding binding,
        GameActionRequest request,
        out string input,
        out string message)
    {
        input = binding.LegacyValue ?? string.Empty;
        message = null;
        var spec = binding.Input;
        if (spec == null)
            return true;

        if (string.Equals(spec.Kind, "number", StringComparison.Ordinal))
        {
            int value;
            if (request.Value.HasValue)
                value = request.Value.Value;
            else if (!int.TryParse(request.Text, out value))
            {
                message = $"{spec.Label} must be a whole number.";
                return false;
            }

            if (spec.Min.HasValue && value < spec.Min.Value)
            {
                message = $"{spec.Label} must be at least {spec.Min.Value}.";
                return false;
            }

            if (spec.Max.HasValue && value > spec.Max.Value)
            {
                message = $"{spec.Label} must be no more than {spec.Max.Value}.";
                return false;
            }

            input = value.ToString();
            return true;
        }

        input = request.Text ?? string.Empty;
        if (spec.Required && string.IsNullOrWhiteSpace(input))
        {
            message = $"{spec.Label} is required.";
            return false;
        }

        if (spec.MaxLength.HasValue && input.Length > spec.MaxLength.Value)
        {
            message = $"{spec.Label} must be {spec.MaxLength.Value} characters or fewer.";
            return false;
        }

        return true;
    }

    private static void SendLegacyInput(string input)
    {
        var game = GameSimulationApp.Instance;
        if (game == null)
            return;

        game.InputManager.ClearBuffer();
        foreach (var character in input ?? string.Empty)
            game.InputManager.AddCharToInputBuffer(character);
        game.InputManager.SendInputBufferAsCommand();
    }

    private static void SelectMenuIndex(int targetIndex)
    {
        var menu = GameSimulationApp.Instance?.ActiveMenu;
        if (menu == null || !menu.HasOptions || targetIndex < 0 || targetIndex >= menu.Options.Count)
            return;

        for (var step = 0; step < menu.Options.Count && menu.SelectedIndex != targetIndex; step++)
            menu.MoveDown();
    }

    private static void InvokeStore(BrowserGame game, string methodName, Entity.Entities? item, int value)
    {
        if (game.CurrentForm == null || item == null)
            throw new InvalidOperationException("The store is no longer active.");

        var method = game.CurrentForm.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(Entity.Entities), typeof(int) },
            null);
        if (method == null)
            throw new InvalidOperationException("The current screen cannot adjust store quantities.");

        method.Invoke(game.CurrentForm, new object[] { item.Value, value });
    }

    private void PulseOnce()
    {
        if (GameSimulationApp.Instance != null)
            GameSimulationApp.Instance.OnTick(true);
    }

    private static GameActionResponseDto Rejected(string code, string message, GameSnapshotDto state)
    {
        return new GameActionResponseDto(false, code, message, state);
    }


    private sealed class BrowserGame(GameSimulationApp simulation)
    {
        public GameSimulationApp Simulation { get; set; } = simulation;
        public IReadOnlyDictionary<string, PresentationActionBinding> Bindings { get; set; } =
            new Dictionary<string, PresentationActionBinding>(StringComparer.Ordinal);
        public string SurfaceKey { get; set; }
        public object CurrentForm { get; set; }
        public long Revision { get; set; }
    }
}
