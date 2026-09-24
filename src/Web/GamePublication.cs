using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OregonTrailDotNet.Web;

/// <summary>One immutable, pre-serialized publication shared by HTTP readers and stream subscribers.</summary>
internal sealed class GamePublication
{
    public GamePublication(GameSnapshotDto state, string journeyId)
    {
        State = state with { JourneyId = journeyId };
        var json = JsonSerializer.Serialize(State, GameJsonContext.Default.GameSnapshotDto);
        Json = Encoding.UTF8.GetBytes(json);
        Event = Encoding.UTF8.GetBytes($"id: {journeyId}:{state.Revision}\nevent: state\ndata: {json}\n\n");
        ETag = $"\"{journeyId}-{state.Revision}\"";
    }

    public GameSnapshotDto State { get; }
    public byte[] Json { get; }
    public byte[] Event { get; }
    public string ETag { get; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GameSnapshotDto))]
[JsonSerializable(typeof(GameActionRequest))]
[JsonSerializable(typeof(GameActionResponseDto))]
internal partial class GameJsonContext : JsonSerializerContext;
