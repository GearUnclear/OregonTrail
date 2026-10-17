using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace OregonTrailDotNet.Web;

/// <summary>
/// Serves index.html with a content fingerprint (<c>?v=</c>) on every local CSS, JS, and SVG reference, so a release
/// can never ship a changed asset under an old cache key. The shell itself is always revalidated; an asset requested
/// with its current fingerprint may be cached for a year. URLs stay relative, so the <c>/oregon/</c> proxy prefix works.
/// Each shell or fingerprinted request stats index.html and its referenced assets (about nine files) and rebuilds the
/// snapshot when one changed, so edits under a running host are picked up; the published layout never changes at runtime.
/// </summary>
internal sealed partial class BrowserShell
{
    public const string ShellCacheControl = "no-cache";
    public const string FingerprintedCacheControl = "public, max-age=31536000, immutable";
    private const int FingerprintLength = 12;

    private readonly IFileProvider _files;
    private volatile Snapshot _current;

    public BrowserShell(IFileProvider files) => _files = files;

    /// <summary>The rewritten shell, rebuilt if index.html or a referenced asset changed on disk since the last build.</summary>
    public Snapshot Current
    {
        get
        {
            var current = _current;
            if (current is null || !current.IsFresh(_files)) _current = current = Build(_files);
            return current;
        }
    }

    public static bool IsShellRequest(HttpRequest request) =>
        (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) &&
        (request.Path.Value is null or "" or "/" || string.Equals(request.Path.Value, "/index.html", StringComparison.OrdinalIgnoreCase));

    public async Task WriteShellAsync(HttpContext context)
    {
        var page = Current;
        var response = context.Response;
        if (page.Html is null)
        {
            response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        response.Headers.CacheControl = ShellCacheControl;
        response.Headers.ETag = page.ETag;
        var request = context.Request;
        if ((HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) && MatchesIfNoneMatch(request, page.ETag))
        {
            response.StatusCode = StatusCodes.Status304NotModified;
            return;
        }

        response.ContentType = "text/html; charset=utf-8";
        // Response compression drops this when it encodes the body; identity and HEAD responses keep it.
        response.ContentLength = page.Html.Length;
        if (!HttpMethods.IsHead(context.Request.Method)) await response.Body.WriteAsync(page.Html, context.RequestAborted);
    }

    /// <summary>If-None-Match uses weak comparison (RFC 9110 13.1.2), so lists, <c>*</c>, and a strong form of the tag all match.</summary>
    private static bool MatchesIfNoneMatch(HttpRequest request, string etag)
    {
        var header = request.Headers.IfNoneMatch;
        if (header.Count == 0 || !EntityTagHeaderValue.TryParseList(header, out var candidates)) return false;
        var current = EntityTagHeaderValue.Parse(etag);
        return candidates.Any(candidate => candidate.Equals(EntityTagHeaderValue.Any) || candidate.Compare(current, useStrongComparison: false));
    }

    /// <summary>Long-lived caching only for a request that names the asset's current fingerprint; everything else revalidates.</summary>
    public string CacheControlFor(HttpRequest request)
    {
        var requested = request.Query["v"];
        if (requested.Count != 1) return ShellCacheControl;
        var path = request.Path.Value?.TrimStart('/') ?? "";
        return Current.Versions.TryGetValue(path, out var version) && string.Equals(version, requested[0], StringComparison.Ordinal)
            ? FingerprintedCacheControl
            : ShellCacheControl;
    }

    /// <summary>A short lowercase hex prefix of the SHA-256 of the asset's bytes.</summary>
    public static string Fingerprint(Stream content) =>
        Convert.ToHexString(SHA256.HashData(content))[..FingerprintLength].ToLowerInvariant();

    /// <summary>
    /// Sets <c>?v=</c> on relative href/src references to .css, .js, and .svg files, replacing any existing <c>?v=</c>.
    /// Absolute, root-relative, scheme, fragment, and other-query URLs are left alone, as is any path whose version is null.
    /// </summary>
    public static string Rewrite(string html, Func<string, string> versionOf) =>
        AssetReference().Replace(html, match =>
        {
            var path = match.Groups["path"].Value;
            if (path.StartsWith('/') || path.Split('/').Contains("..")) return match.Value;
            var version = versionOf(path);
            return version is null ? match.Value : $"{match.Groups["attr"].Value}=\"{path}?v={version}\"";
        });

    private static Snapshot Build(IFileProvider files)
    {
        var index = files.GetFileInfo("index.html");
        var stamps = new List<FileStamp> { FileStamp.Of("index.html", index) };
        var versions = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!index.Exists || index.IsDirectory) return new Snapshot(null, null, versions, stamps);

        string source;
        using (var reader = new StreamReader(index.CreateReadStream(), Encoding.UTF8)) source = reader.ReadToEnd();
        var html = Rewrite(source, path =>
        {
            if (versions.TryGetValue(path, out var known)) return known;
            var asset = files.GetFileInfo(path);
            stamps.Add(FileStamp.Of(path, asset));
            if (!asset.Exists || asset.IsDirectory) return null;
            using var stream = asset.CreateReadStream();
            return versions[path] = Fingerprint(stream);
        });
        var bytes = Encoding.UTF8.GetBytes(html);
        // Weak: response compression serves several encodings of the same representation under this tag.
        var etag = $"W/\"{Fingerprint(new MemoryStream(bytes))}\"";
        return new Snapshot(bytes, etag, versions, stamps);
    }

    [GeneratedRegex(@"\b(?<attr>href|src)=""(?<path>[^""?#:]+\.(?:css|js|svg))(?:\?v=[^""#&]*)?""",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AssetReference();

    internal sealed record Snapshot(byte[] Html, string ETag, IReadOnlyDictionary<string, string> Versions,
        IReadOnlyList<FileStamp> Stamps)
    {
        public bool IsFresh(IFileProvider files) =>
            Stamps.All(stamp => stamp == FileStamp.Of(stamp.Path, files.GetFileInfo(stamp.Path)));
    }

    internal readonly record struct FileStamp(string Path, DateTimeOffset Modified, long Length)
    {
        public static FileStamp Of(string path, IFileInfo file) =>
            file.Exists && !file.IsDirectory ? new FileStamp(path, file.LastModified, file.Length) : new FileStamp(path, default, -1);
    }
}
