using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OregonTrailDotNet;
using OregonTrailDotNet.Web;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static async Task Throws<T>(Func<Task> action) where T : Exception
{
    try { await action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}

if (args.Contains("--creator-rules")) { CreatorTests.RunRules(); return; }
if (args.Contains("--creator-balance")) { CreatorTests.RunBalance(false); return; }
if (args.Contains("--food-rules")) { FoodTests.Run(); return; }
FoodTests.Run();
CreatorTests.RunRules();
CreatorTests.RunCatalog();
CreatorTests.RunBalance();
CryptoTests.Run();

var logger = NullLogger.Instance;
var settings = new GameHostOptions { CommandCapacity = 2, MaximumStreamsPerSession = 2, DisconnectGracePeriod = TimeSpan.Zero };
var domain = new ControlledEngine();
await using (var worker = new JourneyWorker(settings, logger, domain))
{
    var initial = await worker.ReadAsync(default);
    using var first = await worker.SubscribeAsync(default);
    using var second = await worker.SubscribeAsync(default);
    await Throws<GameBusyException>(async () => await worker.SubscribeAsync(default));
    Check((await first.Reader.ReadAsync()).State.Revision == 1, "Subscriber receives initial snapshot");
    domain.Hold = true;
    var command = new GameActionRequest("test", 1, null, null, initial.State.JourneyId);
    var accepted = worker.DispatchAsync(command, default);
    await domain.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
    // Cached reads and other journeys must remain available while this domain operation awaits.
    Check(ReferenceEquals(initial, await worker.ReadAsync(default)), "Reads use the immutable cache");
    await using (var other = new JourneyWorker(settings, logger, new ControlledEngine()))
        Check((await other.ReadAsync(default)).State.Revision == 1, "Another journey is independent");

    using var canceled = new CancellationTokenSource();
    var queued1 = worker.DispatchAsync(command, canceled.Token);
    // One coalesced clock pulse may also occupy the mailbox. Saturation must be bounded in either ordering.
    async Task<bool> QueueDuplicate()
    {
        try { return (await worker.DispatchAsync(command, default)).Accepted; }
        catch (GameBusyException) { return false; }
    }
    var queued2 = QueueDuplicate();
    await Throws<GameBusyException>(() => worker.DispatchAsync(command, default));
    canceled.Cancel();
    await Throws<OperationCanceledException>(() => queued1);
    domain.Release.TrySetResult();
    Check((await accepted).Accepted, "First mutation accepted");
    Check(!await queued2, "Duplicate revision rejected or backpressured");
    Check(domain.Mutations == 1, "Canceled queued work never mutates");
    domain.Hold = false;

    // Leave second's initial snapshot unread while publishing many revisions: only the latest may remain.
    for (var i = 0; i < 20; i++)
    {
        var current = await worker.ReadAsync(default);
        await worker.DispatchAsync(command with { ExpectedRevision = current.State.Revision }, default);
    }
    var newest = await worker.ReadAsync(default);
    Check((await second.Reader.ReadAsync()).State.Revision == newest.State.Revision, "Slow subscribers receive latest state");
    Check(!second.Reader.TryRead(out _), "Subscriber queue remains bounded to one state");
    Check((await worker.DispatchAsync(command with { ExpectedJourneyId = "old-host" }, default)).ErrorCode == "stale-revision",
        "Old host incarnation cannot execute an action");
}
Console.WriteLine("PASS bounded command queue, cancellation, isolated work, cached reads, bounded fanout, incarnation guard");

var idleEngine = new ControlledEngine();
await using (var idle = new JourneyWorker(settings, logger, idleEngine))
{
    await idle.ReadAsync(default);
    await Task.Delay(120);
    Check(idleEngine.Ticks == 0, "Disconnected games do not tick beyond grace period");
    using (var active = await idle.SubscribeAsync(default))
    {
        await Task.Delay(120);
        Check(idleEngine.Ticks > 0, "Subscribed journey advances");
    }
    await Task.Delay(80);
    var ticks = idleEngine.Ticks;
    await Task.Delay(120);
    Check(idleEngine.Ticks == ticks, "Unsubscribing pauses the simulation");
}
Console.WriteLine("PASS active/idle clock lifecycle");

using var logs = LoggerFactory.Create(builder => builder.AddSimpleConsole());
var registry = new GameSession(Options.Create(new GameHostOptions { MaximumSessions = 1 }), logs.CreateLogger<GameSession>());
await registry.StartAsync(default);
var retained = registry.GetOrCreate("first");
try { registry.GetOrCreate("second"); throw new Exception("Capacity was not enforced"); }
catch (GameCapacityException) { }
Check(ReferenceEquals(retained, registry.GetOrCreate("first")), "Capacity must never evict a live journey");
await registry.StopAsync(default);
registry.Dispose();
Console.WriteLine("PASS capacity admission and graceful shutdown");

// Cancel a real legacy engine before its first tick, when its per-trip modules do not yet exist.
await using (var starting = new JourneyWorker(settings, logger)) { }
Check(GameSimulationApp.Instance == null, "Legacy current instance must not escape the worker context");
Console.WriteLine("PASS startup cancellation and context cleanup");

sealed class ControlledEngine : IGameEngine
{
    public bool Hold;
    public int Mutations;
    public int Ticks;
    public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public GameSnapshotDto Snapshot { get; private set; } = new(1, true, null, [], [], null, null,
        new("setup", "test", "Test", "", null, []), null);
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public GameSnapshotDto Tick() { Interlocked.Increment(ref Ticks); return Snapshot; }
    public async Task<GameActionResponseDto> DispatchAsync(GameActionRequest request, CancellationToken cancellationToken)
    {
        if (Hold) { Started.TrySetResult(); await Release.Task.WaitAsync(cancellationToken); }
        if (request.ExpectedRevision != Snapshot.Revision) return new(false, "stale-revision", "", Snapshot);
        Mutations++;
        Snapshot = Snapshot with { Revision = Snapshot.Revision + 1 };
        return new(true, null, null, Snapshot);
    }
    public void Dispose() { }
}
