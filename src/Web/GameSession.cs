using Microsoft.Extensions.Options;

namespace OregonTrailDotNet.Web;

/// <summary>Owns worker lifetimes. The registry gate never surrounds game rules, initialization, or network I/O.</summary>
public sealed class GameSession(IOptions<GameHostOptions> options, ILogger<GameSession> logger) : BackgroundService
{
    private readonly object _registryGate = new();
    private readonly Dictionary<string, JourneyWorker> _games = new(StringComparer.Ordinal);
    private readonly GameHostOptions _options = options.Value;
    private bool _closing;

    internal JourneyWorker GetOrCreate(string sessionId)
    {
        lock (_registryGate)
        {
            if (_closing) throw new GameCapacityException("The game host is restarting. Please reconnect shortly.");
            if (_games.TryGetValue(sessionId, out var existing))
            {
                existing.Touch();
                return existing;
            }
            // Never evict another player's live journey to admit a new visitor.
            if (_games.Count >= _options.MaximumSessions)
                throw new GameCapacityException("The road is busy. Please try starting your journey again shortly.");
            var game = new JourneyWorker(_options, logger);
            _games.Add(sessionId, game);
            return game;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                JourneyWorker[] retired;
                lock (_registryGate)
                {
                    var expired = _games.Where(pair => pair.Value.IsFaulted ||
                        (!pair.Value.HasSubscribers && DateTimeOffset.UtcNow - pair.Value.LastAccessed > _options.SessionLifetime)).ToArray();
                    foreach (var pair in expired) _games.Remove(pair.Key);
                    retired = expired.Select(pair => pair.Value).ToArray();
                }
                foreach (var game in retired) await game.DisposeAsync();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        JourneyWorker[] games;
        lock (_registryGate)
        {
            _closing = true;
            games = _games.Values.ToArray();
            _games.Clear();
        }
        await base.StopAsync(cancellationToken);
        await Task.WhenAll(games.Select(async game => await game.DisposeAsync()));
    }
}
