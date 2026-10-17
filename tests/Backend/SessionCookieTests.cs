using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OregonTrailDotNet.Web;

/// <summary>Issue #63: the session cookie follows the trusted proxy's public base path and the session lifetime.</summary>
internal static class SessionCookieTests
{
    public static async Task Run()
    {
        var lifetime = TimeSpan.FromMinutes(90);

        // A direct local run has no prefix: Path=/, not Secure over plain HTTP, Max-Age from the session lifetime.
        var direct = await Proxied(IPAddress.Loopback, prefix: null, proto: null);
        var issued = SessionCookie.GetOrIssue(direct, lifetime);
        Expect(Guid.TryParseExact(issued, "N", out _), "A new session id is a GUID");
        var cookie = Single(direct, SessionCookie.Name);
        Expect(Has(cookie, "path=/") && Has(cookie, "max-age=5400") && Has(cookie, "httponly") &&
            Has(cookie, "samesite=lax") && !Has(cookie, "secure") && !cookie.Contains("domain=", StringComparison.OrdinalIgnoreCase),
            $"Direct cookie attributes: {cookie}");
        Expect(SetCookies(direct).Length == 1, "No legacy cleanup without a legacy cookie");

        // Apache on loopback reports the stripped /oregon prefix and HTTPS: the cookie is scoped and Secure.
        var proxied = await Proxied(IPAddress.Loopback, "/oregon", "https");
        Expect(proxied.Request.PathBase == "/oregon" && proxied.Request.Path == "/api/game", "Prefix becomes PathBase");
        SessionCookie.GetOrIssue(proxied, lifetime);
        cookie = Single(proxied, SessionCookie.Name);
        Expect(Has(cookie, "path=/oregon/") && Has(cookie, "secure") && Has(cookie, "httponly") &&
            Has(cookie, "samesite=lax") && Has(cookie, "max-age=5400"), $"Proxied cookie attributes: {cookie}");

        // A trailing slash in the forwarded prefix must not produce /oregon//.
        var slashed = await Proxied(IPAddress.Loopback, "/oregon/", "https");
        SessionCookie.GetOrIssue(slashed, lifetime);
        Expect(Has(Single(slashed, SessionCookie.Name), "path=/oregon/"), "Trailing-slash prefix normalizes");

        // Forwarded headers from anyone but a loopback proxy are ignored.
        var spoofed = await Proxied(IPAddress.Parse("203.0.113.7"), "/elsewhere", "https");
        Expect(!spoofed.Request.PathBase.HasValue && !spoofed.Request.IsHttps, "Untrusted forwarded headers are ignored");
        SessionCookie.GetOrIssue(spoofed, lifetime);
        cookie = Single(spoofed, SessionCookie.Name);
        Expect(Has(cookie, "path=/") && !Has(cookie, "secure"), $"Untrusted request keeps the direct cookie: {cookie}");

        // A valid cookie is reused without a new Set-Cookie; a malformed one is replaced.
        var returning = await Proxied(IPAddress.Loopback, "/oregon", "https", $"{SessionCookie.Name}={issued}");
        Expect(SessionCookie.GetOrIssue(returning, lifetime) == issued && SetCookies(returning).Length == 0,
            "A returning browser keeps its session silently");
        var malformed = await Proxied(IPAddress.Loopback, "/oregon", "https", $"{SessionCookie.Name}=not-a-guid");
        Expect(SessionCookie.GetOrIssue(malformed, lifetime) != "not-a-guid" && SetCookies(malformed).Length == 1,
            "A malformed cookie is replaced");

        // A browser from before #63 still holds the old Path=/ cookie. It is ignored and expired at Path=/,
        // and the browser gets a cookie on the narrower path.
        var legacyId = Guid.NewGuid().ToString("N");
        var migrating = await Proxied(IPAddress.Loopback, "/oregon", "https", $"{SessionCookie.LegacyName}={legacyId}");
        var migrated = SessionCookie.GetOrIssue(migrating, lifetime);
        Expect(migrated != legacyId, "The legacy session id is not adopted");
        var expired = Single(migrating, SessionCookie.LegacyName);
        Expect(Has(expired, "path=/") && Has(expired, "expires=thu, 01 jan 1970 00:00:00 gmt") && Has(expired, "secure"),
            $"The legacy cookie is expired at its old path: {expired}");
        Expect(Has(Single(migrating, SessionCookie.Name), "path=/oregon/"), "The migrating browser gets the scoped cookie");
        var both = await Proxied(IPAddress.Loopback, "/oregon", "https",
            $"{SessionCookie.Name}={migrated}; {SessionCookie.LegacyName}={legacyId}");
        Expect(SessionCookie.GetOrIssue(both, lifetime) == migrated && SetCookies(both).Length == 1,
            "With both cookies, the new one wins and only the legacy one is expired");

        // The endpoint takes Max-Age from configuration, not a constant.
        var sessions = new GameSession(Options.Create(new GameHostOptions { SessionLifetime = TimeSpan.FromHours(30) }),
            NullLogger<GameSession>.Instance);
        Expect(sessions.SessionLifetime == TimeSpan.FromHours(30), "GameSession exposes the configured lifetime");
        var configured = await Proxied(IPAddress.Loopback, null, null);
        SessionCookie.GetOrIssue(configured, sessions.SessionLifetime);
        Expect(Has(Single(configured, SessionCookie.Name), "max-age=108000"), "Max-Age follows SessionLifetime");
        Console.WriteLine("PASS session cookie path, Secure, Max-Age, trusted prefix, and legacy cleanup");
    }

    /// <summary>Runs a request through the host's forwarded-header policy, as Program.cs configures it.</summary>
    private static async Task<HttpContext> Proxied(IPAddress remote, string prefix, string proto, string cookie = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remote;
        context.Request.Scheme = "http";
        context.Request.Path = "/api/game";
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.20";
        if (prefix != null) context.Request.Headers["X-Forwarded-Prefix"] = prefix;
        if (proto != null) context.Request.Headers["X-Forwarded-Proto"] = proto;
        if (cookie != null) context.Request.Headers.Cookie = cookie;
        var options = new ForwardedHeadersOptions();
        SessionCookie.ConfigureForwardedHeaders(options);
        await new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options))
            .Invoke(context);
        return context;
    }

    private static string[] SetCookies(HttpContext context) => context.Response.Headers.SetCookie.ToArray();

    private static string Single(HttpContext context, string name)
    {
        var matches = SetCookies(context).Where(value => value.StartsWith(name + "=", StringComparison.Ordinal)).ToArray();
        Expect(matches.Length == 1, $"Expected one {name} Set-Cookie, got {matches.Length}");
        return matches[0];
    }

    private static bool Has(string cookie, string attribute) =>
        cookie.Split(';').Skip(1).Any(part => part.Trim().Equals(attribute, StringComparison.OrdinalIgnoreCase));

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
