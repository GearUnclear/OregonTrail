using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using OregonTrailDotNet.Web;

namespace OregonTrailDotNet;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            WebRootPath = ResolveWebRoot()
        });
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024);
        builder.Services.Configure<ForwardedHeadersOptions>(SessionCookie.ConfigureForwardedHeaders);
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            // Stream frames must flush immediately; compress only cacheable browser assets.
            options.MimeTypes = new[] { "text/html", "text/css", "text/javascript", "application/javascript", "image/svg+xml" };
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        builder.Services.AddOptions<GameHostOptions>().BindConfiguration("GameHost")
            .Validate(options => options.MaximumSessions is > 0 and <= 4096 && options.CommandCapacity is > 0 and <= 1024 &&
                options.MaximumStreamsPerSession is > 0 and <= 16 && options.SessionLifetime > TimeSpan.Zero &&
                options.DisconnectGracePeriod >= TimeSpan.Zero, "Invalid game host limits.").ValidateOnStart();
        builder.Services.AddSingleton<GameSession>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<GameSession>());
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, GameJsonContext.Default));

        var app = builder.Build();
        // Only loopback proxies are trusted (the ASP.NET default). Apache sends X-Forwarded-Proto for the public HTTPS
        // scheme and X-Forwarded-Prefix for the /oregon path it strips; the prefix becomes PathBase and scopes the cookie.
        app.UseForwardedHeaders();
        app.UseResponseCompression();
        // index.html is served with content-hashed asset URLs and is always revalidated; see BrowserShell.
        var shell = new BrowserShell(app.Environment.WebRootFileProvider);
        app.Use((context, next) => BrowserShell.IsShellRequest(context.Request) ? shell.WriteShellAsync(context) : next(context));
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = shell.CacheControlFor(context.Context.Request)
        });
        app.MapGameEndpoints();
        app.MapFallback(shell.WriteShellAsync);
        await app.RunAsync();
    }

    private static string ResolveWebRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "wwwroot"),
            Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")
        };
        // A stale or partial output folder (for example an empty bin/.../wwwroot) must not shadow the real assets.
        return candidates.FirstOrDefault(candidate => File.Exists(Path.Combine(candidate, "index.html"))) ?? candidates[0];
    }
}
