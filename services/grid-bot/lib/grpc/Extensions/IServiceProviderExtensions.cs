namespace Grid.Bot.Grpc;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Authentication;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Discord.WebSocket;

using Logging;
using Utility;

/// <summary>
/// gRPC extensions for the <see cref="IServiceProvider"/>
/// </summary>
public static class IServiceProviderExtensions
{
    /// <summary>
    /// Implement the grid-bot gRPC server into the current service provider.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/></param>
    /// <param name="args">The application arguments.</param>
    public static void UseGrpcServer(this IServiceProvider services, IEnumerable<string> args)
    {
        var grpcSettings = services.GetRequiredService<GrpcSettings>();
        var logger = new Logger(
            name: grpcSettings.GrpcServerLoggerName,
            logLevelGetter: () => grpcSettings.GrpcServerLoggerLevel,
            logToConsole: true,
            logToFileSystem: false
        );

        if (!grpcSettings.GridBotGrpcServerEnabled)
        {
            logger.Warning("The grid-bot gRPC server is disabled in settings, not starting gRPC server!");

            return;
        }

        var client = services.GetRequiredService<DiscordShardedClient>();
        
        logger.Information("Starting gRPC server on {0}", grpcSettings.GridBotGrpcServerEndpoint);

        var builder = WebApplication.CreateBuilder([.. args]);

        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(new MicrosoftLoggerProvider(logger));

        builder.Services.AddSingleton(client);

        builder.Services.AddGrpc();

        if (grpcSettings.GrpcServerUseTls)
        {
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.ConfigureEndpointDefaults(listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;

                    try
                    {
                        listenOptions.UseHttps(grpcSettings.GrpcServerCertificatePath, grpcSettings.GrpcServerCertificatePassword, httpsOptions =>
                        {
                            httpsOptions.SslProtocols = SslProtocols.Tls13 | SslProtocols.Tls12;
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.Warning("Failed to configure gRPC with HTTPS because: {0}. Will resort to insecure host instead!", ex.Message);
                    }
                });
            });

            // set urls
        }
        else
        {
            builder.Services.Configure<KestrelServerOptions>(options =>
            {
                options.ConfigureEndpointDefaults(listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            });
        }

        var app = builder.Build();

        app.MapGrpcService<GridBotGrpcServer>();

        Task.Factory.StartNew(() => app.Run(grpcSettings.GridBotGrpcServerEndpoint), TaskCreationOptions.LongRunning);
    }
}
