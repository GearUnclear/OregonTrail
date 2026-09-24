namespace OregonTrailDotNet.Web;

public sealed class GameHostOptions
{
    public int MaximumSessions { get; set; } = 256;
    public int CommandCapacity { get; set; } = 32;
    public int MaximumStreamsPerSession { get; set; } = 4;
    public TimeSpan SessionLifetime { get; set; } = TimeSpan.FromHours(12);
    public TimeSpan DisconnectGracePeriod { get; set; } = TimeSpan.FromSeconds(30);
}

internal sealed class GameCapacityException(string message) : Exception(message);
internal sealed class GameBusyException(string message) : Exception(message);
