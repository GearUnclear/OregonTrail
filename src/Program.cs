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
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
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
        // Only loopback proxies are trusted by default. Apache sends X-Forwarded-Proto for the public HTTPS path.
        app.UseForwardedHeaders();
        app.UseResponseCompression();
        app.UseDefaultFiles();
        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "no-cache"
        });
        app.MapGameEndpoints();
        app.MapFallbackToFile("index.html");
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
        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }
}
