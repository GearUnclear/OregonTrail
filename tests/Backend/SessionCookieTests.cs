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

        // A loopback sender can put anything in the prefix. Values that are not plain paths are dropped before the
        // framework decodes them into PathBase, so they can neither add cookie attributes nor fail the request.
        foreach (var hostile in new[]
        {
            "/oregon;Domain=evil.example", "/oregon%3BDomain=evil.example", "/oregon%0D%0ASet-Cookie:%20x=1",
            "/oregon%00", "//evil", "/oregon/../", "/oregon/./x", "/..", "/%2e%2e", "oregon", "/", "", " /oregon",
            "/a, /b", "/or egon", "/oregon\\x", "/oregon?x=1", "/oregon#x", "/" + new string('a', 200)
        })
        {
            var attacked = await Proxied(IPAddress.Loopback, hostile, "https");
            Expect(!attacked.Request.PathBase.HasValue, $"Hostile prefix {hostile} is not adopted as PathBase");
            SessionCookie.GetOrIssue(attacked, lifetime);
            cookie = Single(attacked, SessionCookie.Name);
            var attributes = cookie.Split(';').Skip(1).Select(part => part.Trim().ToLowerInvariant()).ToArray();
            Expect(attributes.SequenceEqual(new[] { "max-age=5400", "path=/", "secure", "samesite=lax", "httponly" }),
                $"Hostile prefix {hostile} leaves the default cookie: {cookie}");
        }
        var repeated = await Proxied(IPAddress.Loopback, null, "https", extraPrefixes: new[] { "/oregon", "/oregon" });
        Expect(!repeated.Request.PathBase.HasValue, "A repeated prefix header is dropped");
        foreach (var safe in new[] { "/oregon", "/oregon/", "/games/oregon-trail_2028.v1~x", "/a/b/c/" })
            Expect(SessionCookie.IsSafeBasePath(safe), $"{safe} is a plain base path");
        Expect(SessionCookie.PathFor(new PathString("/oregon;Domain=x")) == "/" &&
            SessionCookie.PathFor(new PathString("/oregon")) == "/oregon/" && SessionCookie.PathFor(PathString.Empty) == "/",
            "PathFor never emits an unsafe base");
        var nested = await Proxied(IPAddress.Loopback, "/games/oregon", "https");
        SessionCookie.GetOrIssue(nested, lifetime);
        Expect(Has(Single(nested, SessionCookie.Name), "path=/games/oregon/"), "A nested prefix is kept");

        // A valid cookie is reused. Actions send no Set-Cookie; reads and stream opens renew it with the same value and
        // a fresh Max-Age, so the cookie slides with the journey's inactivity timeout. A malformed one is replaced.
        var returning = await Proxied(IPAddress.Loopback, "/oregon", "https", $"{SessionCookie.Name}={issued}");
        Expect(SessionCookie.GetOrIssue(returning, lifetime) == issued && SetCookies(returning).Length == 0,
            "A returning browser keeps its session silently on actions");
        var renewing = await Proxied(IPAddress.Loopback, "/oregon", "https", $"{SessionCookie.Name}={issued}");
        Expect(SessionCookie.GetOrIssue(renewing, lifetime, renew: true) == issued, "Renewal keeps the session id");
        cookie = Single(renewing, SessionCookie.Name);
        Expect(cookie.StartsWith($"{SessionCookie.Name}={issued};", StringComparison.Ordinal) && Has(cookie, "max-age=5400") &&
            Has(cookie, "path=/oregon/") && Has(cookie, "secure") && Has(cookie, "httponly") && Has(cookie, "samesite=lax") &&
            SetCookies(renewing).Length == 1, $"Renewal re-sends the same cookie with a fresh Max-Age: {cookie}");
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
        var bothRenewed = await Proxied(IPAddress.Loopback, "/oregon", "https",
            $"{SessionCookie.LegacyName}={legacyId}; {SessionCookie.Name}={migrated}");
        Expect(SessionCookie.GetOrIssue(bothRenewed, lifetime, renew: true) == migrated &&
            Single(bothRenewed, SessionCookie.Name).StartsWith($"{SessionCookie.Name}={migrated};", StringComparison.Ordinal) &&
            Has(Single(bothRenewed, SessionCookie.LegacyName), "path=/"),
            "In either order, the new cookie wins, is renewed, and the legacy one is expired");

        // A browser can hold asphalt_trail_sid at both Path=/ and Path=/oregon/ if the app shipped before Apache sent the
        // prefix. Browsers list the longer path first, and the first value wins, so the scoped cookie is the one used.
        var scopedId = Guid.NewGuid().ToString("N");
        var duplicated = await Proxied(IPAddress.Loopback, "/oregon", "https",
            $"{SessionCookie.Name}={scopedId}; {SessionCookie.Name}={issued}");
        Expect(SessionCookie.GetOrIssue(duplicated, lifetime) == scopedId, "The first of two same-named cookies wins");
        var badFirst = await Proxied(IPAddress.Loopback, "/oregon", "https",
            $"{SessionCookie.Name}=garbage; {SessionCookie.Name}={issued}");
        Expect(SessionCookie.GetOrIssue(badFirst, lifetime) == issued && SetCookies(badFirst).Length == 0,
            "A malformed duplicate does not displace a valid session id");

        // The endpoint takes Max-Age from configuration, not a constant.
        var sessions = new GameSession(Options.Create(new GameHostOptions { SessionLifetime = TimeSpan.FromHours(30) }),
            NullLogger<GameSession>.Instance);
        Expect(sessions.SessionLifetime == TimeSpan.FromHours(30), "GameSession exposes the configured lifetime");
        var configured = await Proxied(IPAddress.Loopback, null, null);
        SessionCookie.GetOrIssue(configured, sessions.SessionLifetime);
        Expect(Has(Single(configured, SessionCookie.Name), "max-age=108000"), "Max-Age follows SessionLifetime");
        Console.WriteLine("PASS session cookie path, Secure, Max-Age, renewal, trusted and sanitized prefix, and legacy cleanup");
    }

    /// <summary>Runs a request through the host's forwarded-header policy, as Program.cs configures it.</summary>
    private static async Task<HttpContext> Proxied(IPAddress remote, string prefix, string proto, string cookie = null,
        string[] extraPrefixes = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = remote;
        context.Request.Scheme = "http";
        context.Request.Path = "/api/game";
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.20";
        if (prefix != null) context.Request.Headers["X-Forwarded-Prefix"] = prefix;
        if (extraPrefixes != null) context.Request.Headers["X-Forwarded-Prefix"] = extraPrefixes;
        if (proto != null) context.Request.Headers["X-Forwarded-Proto"] = proto;
        if (cookie != null) context.Request.Headers.Cookie = cookie;
        var options = new ForwardedHeadersOptions();
        SessionCookie.ConfigureForwardedHeaders(options);
        var forwarded = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        // The same order as Program.cs: the prefix sanitizer runs first.
        await SessionCookie.SanitizeForwardedPrefix(context, forwarded.Invoke);
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
