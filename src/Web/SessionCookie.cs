using Microsoft.AspNetCore.HttpOverrides;
using CookieHeaderValue = Microsoft.Net.Http.Headers.CookieHeaderValue;

namespace OregonTrailDotNet.Web;

/// <summary>
/// Issues the HttpOnly cookie that keys a browser to its journey. The cookie is scoped to the public base path:
/// behind Apache that is the <c>X-Forwarded-Prefix</c> the trusted loopback proxy reports (for example
/// <c>/oregon</c>), so the cookie is not sent to the rest of the site; a direct run keeps <c>Path=/</c>.
/// </summary>
internal static class SessionCookie
{
    public const string Name = "asphalt_trail_sid";

    /// <summary>
    /// The pre-#63 cookie, always issued with <c>Path=/</c>. It is never read: browsers send both cookies when both
    /// exist and the request header carries no path, so a new name avoids guessing which value is which.
    /// </summary>
    public const string LegacyName = "asphalt_trail_session";

    private const string PrefixHeader = "X-Forwarded-Prefix";
    private const int MaximumBasePathLength = 128;

    /// <summary>Applies the host's forwarded-header policy. Only loopback proxies (the ASP.NET default) are trusted.</summary>
    public static void ConfigureForwardedHeaders(ForwardedHeadersOptions options) =>
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedPrefix;

    /// <summary>
    /// Runs before <c>UseForwardedHeaders</c> and drops an <c>X-Forwarded-Prefix</c> that is not a single plain path.
    /// The framework percent-decodes the prefix into <c>PathBase</c>, so without this a value such as
    /// <c>/oregon%3BDomain=x</c> would add cookie attributes, and <c>%00</c> fails the request. A dropped prefix
    /// behaves like a missing one: the cookie falls back to <c>Path=/</c>.
    /// </summary>
    public static Task SanitizeForwardedPrefix(HttpContext context, RequestDelegate next)
    {
        var headers = context.Request.Headers;
        if (headers.TryGetValue(PrefixHeader, out var values) && (values.Count != 1 || !IsSafeBasePath(values[0])))
            headers.Remove(PrefixHeader);
        return next(context);
    }

    /// <summary>
    /// Returns the request's session id, issuing a new cookie whose Max-Age is the session lifetime. With
    /// <paramref name="renew"/>, an existing cookie is re-sent with a fresh Max-Age, so an active player's cookie
    /// slides along with the journey's inactivity timeout instead of expiring a fixed time after the first visit.
    /// </summary>
    public static string GetOrIssue(HttpContext context, TimeSpan lifetime, bool renew = false)
    {
        if (context.Request.Cookies.ContainsKey(LegacyName))
            context.Response.Cookies.Delete(LegacyName, Options(context, "/", null));
        if (FindSessionId(context.Request) is { } existing)
        {
            if (renew) Append(context, existing, lifetime);
            return existing;
        }
        var sessionId = Guid.NewGuid().ToString("N");
        Append(context, sessionId, lifetime);
        return sessionId;
    }

    /// <summary>
    /// The first well-formed session id in header order. A browser can hold the cookie on two paths (for example
    /// <c>/</c> from a release that ran before the proxy sent its prefix, and <c>/oregon/</c>); it lists the longer
    /// path first, while <c>Request.Cookies</c> would keep the last value.
    /// </summary>
    private static string FindSessionId(HttpRequest request)
    {
        if (!CookieHeaderValue.TryParseList(request.Headers.Cookie, out var cookies)) return null;
        foreach (var cookie in cookies)
            if (cookie.Name.Equals(Name, StringComparison.Ordinal) && Guid.TryParseExact(cookie.Value.Value, "N", out _))
                return cookie.Value.Value;
        return null;
    }

    /// <summary>
    /// <c>/oregon</c> becomes <c>/oregon/</c>, matching the canonical URL Apache redirects to; no base is <c>/</c>.
    /// Anything that is not a plain path also gives <c>/</c>, so the base can never add cookie attributes.
    /// </summary>
    public static string PathFor(PathString pathBase) =>
        pathBase.Value is { Length: > 0 } value && IsSafeBasePath(value) ? value.TrimEnd('/') + "/" : "/";

    /// <summary>
    /// One or more <c>/segment</c>s of unreserved URL characters (<c>A-Z a-z 0-9 - . _ ~</c>), none of them empty,
    /// <c>.</c>, or <c>..</c>, optionally ending in one slash. That rules out percent escapes, separators such as
    /// <c>;</c> and <c>,</c>, whitespace, and control characters.
    /// </summary>
    public static bool IsSafeBasePath(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaximumBasePathLength || value[0] != '/') return false;
        var trimmed = value.Length > 1 && value[^1] == '/' ? value[..^1] : value;
        return trimmed[1..].Split('/').All(segment => segment is { Length: > 0 } and not "." and not ".." &&
            segment.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '.' or '_' or '~'));
    }

    private static void Append(HttpContext context, string sessionId, TimeSpan lifetime) =>
        context.Response.Cookies.Append(Name, sessionId, Options(context, PathFor(context.Request.PathBase), lifetime));

    private static CookieOptions Options(HttpContext context, string path, TimeSpan? lifetime) => new()
    {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.Lax,
        // Secure follows the scheme, which behind Apache is the trusted X-Forwarded-Proto.
        Secure = context.Request.IsHttps,
        Path = path,
        MaxAge = lifetime
    };
}
