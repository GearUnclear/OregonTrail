using System.Globalization;

namespace OregonTrailDotNet.Web;

/// <summary>
/// Test-only seeded journeys (#69). With <c>GameHost:TestSeeds</c> on, the request that creates a new journey may send
/// <see cref="Header" /> to fix every random generator the journey owns, so an HTTP test can replay a known path.
/// The browser never sends it. A host with the option off answers any game request that carries it with 400.
/// </summary>
internal static class TestSeeds
{
    public const string Header = "X-Test-Seed";
    private static readonly object ItemKey = new();

    /// <summary>Refuses to start a host with seeds on outside Development, so production can't be configured into it.</summary>
    public static void EnsureAllowed(bool enabled, IHostEnvironment environment)
    {
        if (enabled && !environment.IsDevelopment())
            throw new InvalidOperationException(
                "GameHost:TestSeeds is test-only and requires ASPNETCORE_ENVIRONMENT=Development " +
                $"(this host is '{environment.EnvironmentName}'). Never enable it in production.");
    }

    /// <summary>
    /// Reads the seed header into the request. Returns a 400 result when the header is present but seeds are off, or
    /// when its value isn't a single whole number from 0 to <see cref="int.MaxValue" />. Returns null otherwise.
    /// </summary>
    public static IResult Read(HttpContext context, bool enabled)
    {
        if (!context.Request.Headers.TryGetValue(Header, out var values)) return null;
        if (!enabled)
            return Results.Problem($"{Header} is not accepted on this host.", statusCode: StatusCodes.Status400BadRequest);
        if (values.Count != 1 || !int.TryParse(values[0], NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
            return Results.Problem($"{Header} must be one whole number from 0 to {int.MaxValue}.",
                statusCode: StatusCodes.Status400BadRequest);
        context.Items[ItemKey] = seed;
        return null;
    }

    /// <summary>The seed this request asked for, if any. Only <see cref="Read" /> sets it.</summary>
    public static int? Requested(HttpContext context) => context.Items.TryGetValue(ItemKey, out var seed) ? (int)seed : null;
}
