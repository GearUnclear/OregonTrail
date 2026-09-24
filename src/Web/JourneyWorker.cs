using System.Collections.Concurrent;
using System.Threading.Channels;

namespace OregonTrailDotNet.Web;

/// <summary>
/// A single consumer owns each simulation. Commands and clock pulses share a bounded mailbox;
/// readers use a cached publication and slow stream readers keep only the most recent snapshot.
/// </summary>
internal sealed class JourneyWorker : IAsyncDisposable
{
    private readonly IGameEngine _engine;
    private readonly GameHostOptions _options;
    private readonly ILogger _logger;
    private readonly Channel<Work> _mailbox;
    private readonly Channel<bool> _clockWake = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.DropWrite,
        AllowSynchronousContinuations = false
    });
    private readonly CancellationTokenSource _stopping = new();
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly ConcurrentDictionary<Guid, Channel<GamePublication>> _subscribers = new();
    private readonly object _subscriptionsGate = new();
    private readonly Task _runner;
    private readonly string _journeyId = Guid.NewGuid().ToString("N");
    private GamePublication _publication;
    private long _lastAccessed = DateTimeOffset.UtcNow.UtcTicks;
    private int _tickQueued;
    private int _stopped;
    private bool _faulted;

    public JourneyWorker(GameHostOptions options, ILogger logger, IGameEngine engine = null)
    {
        _engine = engine ?? new GameEngine();
        _options = options;
        _logger = logger;
        _mailbox = Channel.CreateBounded<Work>(new BoundedChannelOptions(options.CommandCapacity)
        {
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
        _runner = Task.Run(RunAsync);
    }

    public DateTimeOffset LastAccessed => new(Interlocked.Read(ref _lastAccessed), TimeSpan.Zero);
    public bool HasSubscribers => !_subscribers.IsEmpty;
    public bool IsFaulted => Volatile.Read(ref _faulted);
    private bool ShouldPulse => HasSubscribers || DateTimeOffset.UtcNow - LastAccessed < _options.DisconnectGracePeriod;
    public void Touch()
    {
        Interlocked.Exchange(ref _lastAccessed, DateTimeOffset.UtcNow.UtcTicks);
        _clockWake.Writer.TryWrite(true);
    }

    public async Task<GamePublication> ReadAsync(CancellationToken cancellationToken)
    {
        Touch();
        await _ready.Task.WaitAsync(cancellationToken);
        if (Volatile.Read(ref _stopped) != 0) throw new GameBusyException("The journey is reconnecting.");
        return Volatile.Read(ref _publication);
    }

    public async Task<GameActionResponseDto> DispatchAsync(GameActionRequest request, CancellationToken cancellationToken)
    {
        var current = await ReadAsync(cancellationToken);
        if (request?.ExpectedJourneyId != _journeyId)
            return new GameActionResponseDto(false, "stale-revision", "This journey has restarted. Review the current screen.", current.State);
        var completion = new TaskCompletionSource<GameActionResponseDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_mailbox.Writer.TryWrite(new Work(request, completion, cancellationToken)))
            throw new GameBusyException("This journey is busy. Please try again shortly.");
        // A disconnected client must not automatically replay a command whose acknowledgement it missed.
        return await completion.Task.WaitAsync(cancellationToken);
    }

    public async Task<Subscription> SubscribeAsync(CancellationToken cancellationToken)
    {
        await ReadAsync(cancellationToken);
        lock (_subscriptionsGate)
        {
            if (Volatile.Read(ref _stopped) != 0)
                throw new GameBusyException("The journey is reconnecting.");
            if (_subscribers.Count >= _options.MaximumStreamsPerSession)
                throw new GameBusyException("Too many open tabs for this journey. Close a tab and try again.");
            var channel = Channel.CreateBounded<GamePublication>(new BoundedChannelOptions(1)
            {
                SingleReader = true,
                FullMode = BoundedChannelFullMode.DropOldest,
                AllowSynchronousContinuations = false
            });
            var id = Guid.NewGuid();
            // Publication and registration share a short gate: the initial state cannot overtake an update.
            _subscribers[id] = channel;
            _clockWake.Writer.TryWrite(true);
            channel.Writer.TryWrite(Volatile.Read(ref _publication));
            return new Subscription(channel.Reader, () =>
            {
                lock (_subscriptionsGate)
                {
                    if (_subscribers.TryRemove(id, out var removed)) removed.Writer.TryComplete();
                    Touch();
                }
            });
        }
    }

    private void Publish(GameSnapshotDto state)
    {
        if (_publication?.State.Revision == state.Revision) return;
        var publication = new GamePublication(state, _journeyId);
        lock (_subscriptionsGate)
        {
            Volatile.Write(ref _publication, publication);
            foreach (var subscriber in _subscribers.Values) subscriber.Writer.TryWrite(publication);
        }
    }

    private async Task RunAsync()
    {
        Task clock = Task.CompletedTask;
        try
        {
            await _engine.InitializeAsync(_stopping.Token);
            Publish(_engine.Snapshot);
            _ready.TrySetResult();
            clock = RunClockAsync();
            await foreach (var work in _mailbox.Reader.ReadAllAsync(_stopping.Token))
            {
                if (work.Completion == null)
                {
                    Interlocked.Exchange(ref _tickQueued, 0);
                    if (ShouldPulse)
                        Publish(_engine.Tick());
                    continue;
                }
                if (work.CancellationToken.IsCancellationRequested)
                {
                    work.Completion.TrySetCanceled(work.CancellationToken);
                    continue;
                }
                try
                {
                    var response = await _engine.DispatchAsync(work.Request, _stopping.Token);
                    Publish(response.State);
                    work.Completion.TrySetResult(response with { State = _publication.State });
                }
                catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
                {
                    work.Completion.TrySetCanceled(_stopping.Token);
                    throw;
                }
                catch (Exception exception)
                {
                    // A partially applied rule cannot safely accept more actions. Other workers remain live.
                    work.Completion.TrySetException(new GameBusyException("This journey could not continue. Reconnect to start again."));
                    throw new InvalidOperationException("Journey action failed.", exception);
                }
            }
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Volatile.Write(ref _faulted, true);
            _logger.LogError(exception, "A journey worker failed");
            _ready.TrySetException(new GameBusyException("The journey could not start. Please reconnect."));
        }
        finally
        {
            Interlocked.Exchange(ref _stopped, 1);
            _mailbox.Writer.TryComplete();
            _stopping.Cancel();
            await clock;
            while (_mailbox.Reader.TryRead(out var pending))
                pending.Completion?.TrySetException(new GameBusyException("The journey is reconnecting."));
            lock (_subscriptionsGate)
            {
                foreach (var subscriber in _subscribers.Values) subscriber.Writer.TryComplete();
                _subscribers.Clear();
            }
            _ready.TrySetCanceled();
            _engine.Dispose();
        }
    }

    private async Task RunClockAsync()
    {
        try
        {
            while (!_stopping.IsCancellationRequested)
            {
                if (!ShouldPulse)
                {
                    // Fully idle journeys await activity instead of allocating clock work or polling a timer.
                    await _clockWake.Reader.ReadAsync(_stopping.Token);
                    continue;
                }
                await Task.Delay(50, _stopping.Token);
                if (!ShouldPulse) continue;
                if (Interlocked.Exchange(ref _tickQueued, 1) == 0 && !_mailbox.Writer.TryWrite(new Work()))
                    Interlocked.Exchange(ref _tickQueued, 0);
            }
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested) { }
    }

    public async ValueTask DisposeAsync()
    {
        _stopping.Cancel();
        await _runner;
        _stopping.Dispose();
    }

    private sealed record Work(GameActionRequest Request = null,
        TaskCompletionSource<GameActionResponseDto> Completion = null, CancellationToken CancellationToken = default);

    internal sealed class Subscription(ChannelReader<GamePublication> reader, Action unsubscribe) : IDisposable
    {
        private Action _unsubscribe = unsubscribe;
        public ChannelReader<GamePublication> Reader { get; } = reader;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
