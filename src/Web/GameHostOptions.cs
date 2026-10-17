namespace OregonTrailDotNet.Web;

public sealed class GameHostOptions
{
    public int MaximumSessions { get; set; } = 256;
    public int CommandCapacity { get; set; } = 32;
    public int MaximumStreamsPerSession { get; set; } = 4;
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan DisconnectGracePeriod { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Test-only (#69): lets the request that starts a new journey fix its random seed with the X-Test-Seed header.
    /// Off by default. The host refuses to start with it on unless the environment is Development.
    /// </summary>
    public bool TestSeeds { get; set; }
}

internal sealed class GameCapacityException(string message) : Exception(message);
internal sealed class GameBusyException(string message) : Exception(message);
internal sealed class GameSeedException(string message) : Exception(message);
