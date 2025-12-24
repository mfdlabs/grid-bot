namespace Grid.Bot.Web;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Prometheus;

using Logging;
using Utility;

using Routes;
using Middleware;
using Extensions;

/// <summary>
/// gRPC extensions for the <see cref="IServiceProvider"/>
/// </summary>
public static class IServiceProviderExtensions
{
    /// <summary>
    /// Implement the grid-service WebSrv server into the current service provider.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/></param>
    /// <param name="args">The application arguments.</param>
    public static void UseWebServer(this IServiceProvider services, IEnumerable<string> args)
    {
        var webSettings = services.GetRequiredService<WebSettings>();
        var logger = new Logger(
            name: webSettings.WebServerLoggerName,
            logLevelGetter: () => webSettings.WebServerLoggerLevel,
            logToConsole: true,
            logToFileSystem: true
        );

        if (!webSettings.IsWebServerEnabled)
        {
            logger.Warning("The grid-bot web server is disabled in settings, not starting web server!");

            return;
        }

        var clientSettingsFactory = services.GetRequiredService<IClientSettingsFactory>();

        logger.Information("Starting web server on {0}", webSettings.WebServerBindAddress);

        var builder = WebApplication.CreateBuilder([.. args]);

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new MicrosoftLoggerProvider(logger));

        var avatarSettings = services.GetRequiredService<AvatarSettings>();
        var clientSettingsSettings = services.GetRequiredService<ClientSettingsSettings>();

        builder.Services.AddSingleton<Logging.ILogger>(logger);
        builder.Services.AddSingleton(webSettings);
        builder.Services.AddSingleton(clientSettingsFactory);
        builder.Services.AddSingleton(clientSettingsSettings);
        builder.Services.AddSingleton(avatarSettings);

        builder.Services.AddHttpClient();

        // Routes
        builder.Services.AddSingleton<Avatar>();
        builder.Services.AddSingleton<ClientSettings>();
        builder.Services.AddSingleton<VersionCompatibility>();

        builder.Services.AddRouting();
        
        // Conmfigure case insensitive routing
        builder.Services.Configure<RouteOptions>(options =>
        {
            options.LowercaseQueryStrings = true;
            options.LowercaseUrls = true;
        });

        builder.Services.AddHealthChecks();
        builder.Services.AddMetrics();

        if (webSettings.WebServerUseTls)
        {
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.ConfigureEndpointDefaults(listenOptions =>
                {
                    try
                    {
                        listenOptions.UseHttps(webSettings.WebServerCertificatePath, webSettings.WebServerCertificatePassword);
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                    }
                    catch (Exception ex)
                    {
                        logger.Warning("Failed to load TLS certificate: {0}. Will fall back to HTTP.", ex.Message);
                    }
                });
            });
        }

        if (webSettings.IsWebServerBehindProxy)
        {
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();

                foreach (var ip in webSettings.WebServerAllowedProxyRanges)
                    options.KnownNetworks.Add(IPNetwork.Parse(ip));
            });
        }

        var app = builder.Build();

        app.UseMiddleware<HttpServerRequestLoggingMiddleware>();
        app.UseMiddleware<UnhandledExceptionMiddleware>();

        app.UseRouting();
        
        app.UseHealthChecks("/health");

        app.UseTelemetry();

        if (webSettings.IsWebServerBehindProxy)
            app.UseForwardedHeaders();

        app.MapFallback(async context =>
        {
            context.Response.StatusCode = 404;

            await context.Response.WriteRbxError(string.Empty);
        });

        var avatar = app.Services.GetRequiredService<Avatar>();
        var clientSettings = app.Services.GetRequiredService<ClientSettings>();
        var versionCompatibility = app.Services.GetRequiredService<VersionCompatibility>();

        // Avatar routes
        app.MapGet("/v1/avatar-fetch", avatar.GetAvatarFetch);

        // Client settings routes
        app.MapGet("/v1/settings/application", clientSettings.GetApplicationSettings);

        // Version compatibility routes
        app.MapGet("/GetAllowedMd5Hashes", versionCompatibility.GetAllowedMd5Hashes);

        // Start the web server
        Task.Factory.StartNew(() => app.Run(webSettings.WebServerBindAddress), TaskCreationOptions.LongRunning);
    }
}
