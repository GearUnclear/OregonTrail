using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using OregonTrailDotNet.Web;

internal static class BrowserShellTests
{
    public static void Run()
    {
        var rewritten = BrowserShell.Rewrite(
            "<link href=\"styles.css?v=old\"><script src=\"app.js\"></script><script src=\"/root.js\"></script>" +
            "<script src=\"https://cdn.example/x.js\"></script><link href=\"favicon.svg\"><link href=\"fonts/a.woff2\">" +
            "<script src=\"missing.js\"></script><script src=\"../up.js\"></script><a href=\"q.js?x=1\">",
            path => path == "missing.js" ? null : "h-" + path);
        Expect(rewritten == "<link href=\"styles.css?v=h-styles.css\"><script src=\"app.js?v=h-app.js\"></script>" +
            "<script src=\"/root.js\"></script><script src=\"https://cdn.example/x.js\"></script>" +
            "<link href=\"favicon.svg?v=h-favicon.svg\"><link href=\"fonts/a.woff2\"><script src=\"missing.js\"></script>" +
            "<script src=\"../up.js\"></script><a href=\"q.js?x=1\">", $"Unexpected rewrite: {rewritten}");

        var root = Directory.CreateTempSubdirectory("oregon-shell-");
        try
        {
            File.WriteAllText(Path.Combine(root.FullName, "index.html"),
                "<link rel=\"stylesheet\" href=\"styles.css\" /><script defer src=\"app.js?v=hand-typed\"></script>");
            File.WriteAllText(Path.Combine(root.FullName, "styles.css"), "body{}");
            File.WriteAllText(Path.Combine(root.FullName, "app.js"), "console.log(1);");
            using var files = new PhysicalFileProvider(root.FullName);
            var shell = new BrowserShell(files);
            var first = shell.Current;
            var html = Encoding.UTF8.GetString(first.Html);
            var appVersion = Sha("console.log(1);");
            Expect(html.Contains($"href=\"styles.css?v={Sha("body{}")}\"") && html.Contains($"src=\"app.js?v={appVersion}\""),
                $"Shell must carry content hashes: {html}");
            Expect(ReferenceEquals(first, shell.Current), "An unchanged shell is cached");
            Expect(shell.CacheControlFor(Request("/app.js", appVersion)) == BrowserShell.FingerprintedCacheControl,
                "Current fingerprint is long-cacheable");
            Expect(shell.CacheControlFor(Request("/app.js", "stale")) == BrowserShell.ShellCacheControl, "Stale fingerprint revalidates");
            Expect(shell.CacheControlFor(Request("/app.js", null)) == BrowserShell.ShellCacheControl, "Unversioned asset revalidates");
            Expect(shell.CacheControlFor(Request("/fonts/a.woff2", appVersion)) == BrowserShell.ShellCacheControl,
                "Untracked asset revalidates");

            // Editing an asset under a running host must change its fingerprint, never pin new bytes under the old one.
            var app = Path.Combine(root.FullName, "app.js");
            File.WriteAllText(app, "console.log(22);");
            File.SetLastWriteTimeUtc(app, DateTime.UtcNow.AddMinutes(1));
            var second = shell.Current;
            Expect(!ReferenceEquals(first, second) && Encoding.UTF8.GetString(second.Html).Contains($"app.js?v={Sha("console.log(22);")}"),
                "Changed asset is re-fingerprinted");
            Expect(second.ETag != first.ETag && second.ETag.StartsWith("W/\""), "Shell ETag follows its content");
            Expect(shell.CacheControlFor(Request("/app.js", appVersion)) == BrowserShell.ShellCacheControl,
                "Superseded fingerprint is no longer long-cacheable");

            // Conditional requests use weak comparison: exact, listed, strong-form, and * all revalidate to 304.
            var tag = second.ETag;
            foreach (var ifNoneMatch in new[] { tag, $"\"other\", {tag}", tag[2..], "*" })
                Expect(Serve(shell, "GET", ifNoneMatch).StatusCode == StatusCodes.Status304NotModified, $"304 for If-None-Match {ifNoneMatch}");
            var full = Serve(shell, "GET", first.ETag);
            Expect(full.StatusCode == StatusCodes.Status200OK && full.ContentLength == second.Html.Length &&
                ((MemoryStream)full.Body).ToArray().SequenceEqual(second.Html), "A superseded ETag gets the full current shell");
            var head = Serve(shell, "HEAD", null);
            Expect(head.StatusCode == StatusCodes.Status200OK && head.ContentLength == second.Html.Length && head.Body.Length == 0,
                "HEAD reports the length without a body");
            Expect(Serve(shell, "POST", tag).StatusCode == StatusCodes.Status200OK, "Only GET and HEAD revalidate to 304");
        }
        finally
        {
            root.Delete(true);
        }

        foreach (var (method, path, expected) in new[]
        {
            ("GET", "/", true), ("HEAD", "/index.html", true), ("GET", "/INDEX.HTML", true),
            ("POST", "/", false), ("GET", "/app.js", false), ("GET", "/api/game", false)
        })
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;
            Expect(BrowserShell.IsShellRequest(context.Request) == expected, $"Shell routing for {method} {path}");
        }

        Console.WriteLine("PASS browser shell fingerprints local assets, caches only current fingerprints, and rebuilds on change");
    }

    private static HttpRequest Request(string path, string version)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (version is not null) context.Request.QueryString = new QueryString($"?v={version}");
        return context.Request;
    }

    private static HttpResponse Serve(BrowserShell shell, string method, string ifNoneMatch)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = "/";
        if (ifNoneMatch is not null) context.Request.Headers.IfNoneMatch = ifNoneMatch;
        context.Response.Body = new MemoryStream();
        shell.WriteShellAsync(context).GetAwaiter().GetResult();
        return context.Response;
    }

    private static string Sha(string content) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)))[..12].ToLowerInvariant();

    private static void Expect(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
