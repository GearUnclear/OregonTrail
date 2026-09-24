using Microsoft.AspNetCore.Http.Features;

namespace OregonTrailDotNet.Web;

internal static class GameEndpoints
{
    public static void MapGameEndpoints(this WebApplication app)
    {
        app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
        var api = app.MapGroup("/api/game");
        api.AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            context.Response.Headers.CacheControl = "private, no-store";
            try { return await next(invocation); }
            catch (GameCapacityException exception)
            {
                context.Response.Headers.RetryAfter = "30";
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
            catch (GameBusyException exception)
            {
                context.Response.Headers.RetryAfter = "3";
                return Results.Problem(exception.Message, statusCode: StatusCodes.Status429TooManyRequests);
            }
        });

        api.MapGet("", async (HttpContext context, GameSession sessions, CancellationToken cancellationToken) =>
        {
            var publication = await sessions.GetOrCreate(GetSessionId(context)).ReadAsync(cancellationToken);
            context.Response.Headers.ETag = publication.ETag;
            return context.Request.Headers.IfNoneMatch.Contains(publication.ETag)
                ? Results.StatusCode(StatusCodes.Status304NotModified)
                : Results.Bytes(publication.Json, "application/json; charset=utf-8");
        });

        api.MapPost("/actions", async (HttpContext context, GameActionRequest request,
            GameSession sessions, CancellationToken cancellationToken) =>
        {
            var response = await sessions.GetOrCreate(GetSessionId(context)).DispatchAsync(request, cancellationToken);
            var status = response.ErrorCode switch
            {
                "stale-revision" => StatusCodes.Status409Conflict,
                null => StatusCodes.Status200OK,
                _ => StatusCodes.Status400BadRequest
            };
            return Results.Json(response, GameJsonContext.Default.GameActionResponseDto, statusCode: status);
        });

        api.MapGet("/events", StreamAsync);
        app.Map("/api/{**path}", () => Results.Problem("Unknown API endpoint.", statusCode: StatusCodes.Status404NotFound));
    }

    private static async Task StreamAsync(HttpContext context, GameSession sessions, CancellationToken cancellationToken)
    {
        // Subscribe before sending headers so capacity errors remain normal JSON HTTP errors.
        using var subscription = await sessions.GetOrCreate(GetSessionId(context)).SubscribeAsync(cancellationToken);
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "private, no-cache, no-transform";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();
        try
        {
            await context.Response.WriteAsync("retry: 3000\n\n", cancellationToken);
            // Exactly one pending read and one heartbeat timer per stream, including quiet menu screens.
            using var heartbeat = new PeriodicTimer(TimeSpan.FromSeconds(15));
            var pendingRead = subscription.Reader.WaitToReadAsync(cancellationToken).AsTask();
            var pendingHeartbeat = heartbeat.WaitForNextTickAsync(cancellationToken).AsTask();
            while (!cancellationToken.IsCancellationRequested)
            {
                var completed = await Task.WhenAny(pendingRead, pendingHeartbeat);
                if (completed == pendingRead)
                {
                    if (!await pendingRead) break;
                    while (subscription.Reader.TryRead(out var publication))
                        await context.Response.Body.WriteAsync(publication.Event, cancellationToken);
                    pendingRead = subscription.Reader.WaitToReadAsync(cancellationToken).AsTask();
                }
                else
                {
                    if (!await pendingHeartbeat) break;
                    await context.Response.WriteAsync("event: heartbeat\ndata: {}\n\n", cancellationToken);
                    pendingHeartbeat = heartbeat.WaitForNextTickAsync(cancellationToken).AsTask();
                }
                await context.Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (IOException) when (cancellationToken.IsCancellationRequested) { }
    }

    private static string GetSessionId(HttpContext context)
    {
        const string cookieName = "asphalt_trail_session";
        if (context.Request.Cookies.TryGetValue(cookieName, out var existing) &&
            Guid.TryParseExact(existing, "N", out _)) return existing;
        var sessionId = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(cookieName, sessionId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Path = context.Request.PathBase.HasValue ? context.Request.PathBase.Value + "/" : "/",
            MaxAge = TimeSpan.FromHours(12)
        });
        return sessionId;
    }
}
