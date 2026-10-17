using Microsoft.AspNetCore.HttpOverrides;

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

    /// <summary>Applies the host's forwarded-header policy. Only loopback proxies (the ASP.NET default) are trusted.</summary>
    public static void ConfigureForwardedHeaders(ForwardedHeadersOptions options) =>
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedPrefix;

    /// <summary>Returns the request's session id, issuing a new cookie whose Max-Age is the session lifetime.</summary>
    public static string GetOrIssue(HttpContext context, TimeSpan lifetime)
    {
        if (context.Request.Cookies.ContainsKey(LegacyName))
            context.Response.Cookies.Delete(LegacyName, Options(context, "/", null));
        if (context.Request.Cookies.TryGetValue(Name, out var existing) && Guid.TryParseExact(existing, "N", out _))
            return existing;
        var sessionId = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(Name, sessionId, Options(context, PathFor(context.Request.PathBase), lifetime));
        return sessionId;
    }

    /// <summary><c>/oregon</c> becomes <c>/oregon/</c>, matching the canonical URL Apache redirects to; no base is <c>/</c>.</summary>
    public static string PathFor(PathString pathBase) => pathBase.ToUriComponent().TrimEnd('/') + "/";

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
