﻿namespace Grid.Bot;

using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;

using Grpc.Core;
using Grpc.Net.Client;

using Logging;

using V1;
using Events;
using Prometheus;

internal static class Runner
{
#if DEBUG
    private const string _debugMode = "WARNING: RUNNING IN DEBUG MODE, THIS CAN POTENTIALLY LEAK MORE INFORMATION " +
                                     "THAN NEEDED, PLEASE RUN THIS ON RELEASE FOR PRODUCTION SCENARIOS.";
#endif

    public static void Invoke(string[] args)
    {
        InvokeAsync(args).Wait();

        Environment.Exit(0);
    }

    private static ServiceProvider InitializeServices()
    {
        var services = new ServiceCollection();
        var settings = new Settings();

        services.AddSingleton<ISettings>(settings);

        var logger = new Logger(
            name: settings.DefaultLoggerName,
            logLevel: settings.DefaultLoggerLevel,
            logToConsole: true
        );

        services.AddSingleton<ILogger>(logger);

        var informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

        logger.Information($"Starting Grid.Bot.Recovery, Version = {informationalVersion}");

#if DEBUG

        Logger.GlobalLogPrefixes.Add(() => informationalVersion);
#endif

        var config = new DiscordSocketConfig()
        {
            GatewayIntents =
                GatewayIntents.GuildMessages
                | GatewayIntents.DirectMessages
                | GatewayIntents.MessageContent,
            ConnectionTimeout = int.MaxValue, // Temp until discord-net/Discord.Net#2743 is fixed
            LogLevel = LogSeverity.Debug,
        };

        services.AddSingleton(config).AddSingleton<DiscordShardedClient>();

        // Event Handlers
        services.AddSingleton<OnLogMessage>()
            .AddSingleton<OnMessage>()
            .AddSingleton<OnInteraction>()
            .AddSingleton<OnShardReady>()
            .AddSingleton<IBotManager, BotManager>();

        // Http Client Factory
        services.AddHttpClient();

        services.AddSingleton(_ => GetGridBotClient(settings));
        services.AddSingleton<BotCheckWorker>();

        return services.BuildServiceProvider();
    }

    private static GridBotAPI.GridBotAPIClient GetGridBotClient(ISettings settings)
    {
        if (settings.StandaloneMode)
            return null;

        var channel = GrpcChannel.ForAddress(settings.GridBotEndpoint);

        System.AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        return new GridBotAPI.GridBotAPIClient(channel);
    }

    private static async Task InvokeAsync(IEnumerable<string> args)
    {
        var services = InitializeServices();
        var logger = services.GetRequiredService<ILogger>();
        var settings = services.GetRequiredService<ISettings>();


#if DEBUG
        logger.Warning(_debugMode);
#endif


        try
        {
            new KestrelMetricServer(
                port: settings.MetricsServerPort
            ).Start();
        }
        catch (Exception e)
        {
            logger.Warning("Failed to start metrics server: {0}", e.Message);
        }

        if (settings.StandaloneMode)
        {
            var botManager = services.GetRequiredService<IBotManager>();
            await botManager.StartAsync().ConfigureAwait(false);

            await Task.Delay(Timeout.Infinite).ConfigureAwait(false);

            return;
        }

        var botCheckWorker = services.GetRequiredService<BotCheckWorker>();

        await botCheckWorker.StartAsync().ConfigureAwait(false);

        await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
    }
}
