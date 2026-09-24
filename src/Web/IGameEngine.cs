namespace OregonTrailDotNet.Web;

/// <summary>The journey worker schedules domain operations; the adapter owns the rules and their presentation.</summary>
internal interface IGameEngine : IDisposable
{
    GameSnapshotDto Snapshot { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
    GameSnapshotDto Tick();
    Task<GameActionResponseDto> DispatchAsync(GameActionRequest request, CancellationToken cancellationToken);
}
