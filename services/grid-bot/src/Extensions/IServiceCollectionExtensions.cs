namespace Grid.Bot.Extensions;

using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Discord;
using Discord.Rest;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using Logging;

using Events;
using Utility;
using Commands;
using ClientSettings;
using UnifiedCommands.Public;

using Grid.JobManagement;
using Grid.PortManagement;
using Grid.ProcessManagement;

using EnvironmentProvider = Grid.Bot.EnvironmentDataProvider;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Add the unified command service to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddUnifiedCommands(this IServiceCollection services)
        => services
            .AddSingleton<Support>()
            .AddSingleton<Render>()
            .AddSingleton<ExecuteScript>();

    /// <summary>
    /// Add all utilities to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IBacktraceUtility, BacktraceUtility>()
            .AddSingleton<IAdminUtility, AdminUtility>()
            .AddSingleton<IDiscordWebhookAlertManager, DiscordWebhookAlertManager>()
            .AddSingleton<IPerUserContextLoggerFactory, PerUserContextLoggerFactory>()
            .AddSingleton<IGridServerFileHelper, GridServerFileHelper>()
            .AddSingleton<IVaultFactory, VaultFactory>();

        return services;
    }

    /// <summary>
    /// Add the global logger to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGlobalLogger(this IServiceCollection services)
    {
        services.AddSingleton<ILogger>(provider =>
        {
            var globalOptions = provider
                .GetRequiredService<IOptionsMonitor<GlobalOptions>>();

            return new Logger(
                name: globalOptions.CurrentValue.DefaultLoggerName,
                logLevelGetter: () => globalOptions.CurrentValue.DefaultLoggerLevel,
                logToConsole: globalOptions.CurrentValue.DefaultLoggerLogToConsole
            );
        });

        return services;
    }

    /// <summary>
    /// Adds the job manager to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddJobManager(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();

        var gridOptions = provider.GetRequiredService<IOptionsMonitor<GridOptions>>();
        var gridSettings = gridOptions.CurrentValue;

#if DEBUG
        if (gridSettings.DebugUseNoopJobManager)
        {
            services.AddSingleton<IJobManager, NoopJobManager>();

            return services;
        }
#endif

        var logger = new Logger(
            name: gridSettings.JobManagerLoggerName,
            logLevelGetter: () => gridOptions.CurrentValue.JobManagerLogLevel,
            logToConsole: gridSettings.JobManagerLogToConsole
        );

        var clientSettingsFactory = services
            .BuildServiceProvider()
            .GetRequiredService<IClientSettingsFactory>();

        var clientSettingsClient = new ClientSettingsFactoryProxyClient(clientSettingsFactory);

        var portAllocator = new PortAllocator(logger);
        var jobManagerFactory = new JobManagerGridServerFactory();

        var jobManagerGridServer = jobManagerFactory.GetJobManager(
            logger,
            clientSettingsClient,
            provider.GetRequiredService<GridServerSettings>()
        );

        jobManagerGridServer.Start();

        services.AddSingleton<IJobManagerGridServer>(jobManagerGridServer);
        services.AddSingleton<IJobManager, JobManager>();

        return services;
    }

    /// <summary>
    /// Adds the in-memory rate limiters to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
        => services.AddSingleton<IRateLimiterRegistry, RateLimiterRegistry>();

    /// <summary>
    /// Adds all client settings related components to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddClientSettings(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger>();
        var clientSettingsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<ClientSettingsOptions>>();
        var clientSettingsSettings = clientSettingsOptions.CurrentValue;
        var vaultFactory = serviceProvider.GetRequiredService<IVaultFactory>();
        var globalOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GlobalOptions>>().CurrentValue;

        var vaultClient = clientSettingsSettings.ClientSettingsViaVault
            ? vaultFactory.CreateClient(
                clientSettingsSettings.ClientSettingsVaultAddress ?? globalOptions.VaultAddress,
                clientSettingsSettings.ClientSettingsVaultToken ?? globalOptions.VaultCredential)
            : null;

        var clientSettingsFactory = new ClientSettingsFactory(
            vaultClient,
            logger,
            clientSettingsOptions
        );

        services.AddSingleton<IClientSettingsFactory>(clientSettingsFactory);

        return services;
    }

    /// <summary>
    /// Adds all Discord related components to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDiscord(this IServiceCollection services)
    {
        var socketConfig = new DiscordSocketConfig()
        {
            GatewayIntents =
                GatewayIntents.GuildMessages
                | GatewayIntents.DirectMessages
                | GatewayIntents.Guilds
                | GatewayIntents.MessageContent,
            ConnectionTimeout = int.MaxValue, // Temp until discord-net/Discord.Net#2743 is fixed
#if DEBUG || DEBUG_LOGGING_IN_PROD
            LogLevel = LogSeverity.Debug,
#else
            LogGatewayIntentWarnings = false,
            SuppressUnknownDispatchWarnings = true,
#endif
        };

        var interactionServiceConfig = new InteractionServiceConfig()
        {
            LogLevel = LogSeverity.Debug,
            ThrowOnError = false
        };

        var commandServiceConfig = new CommandServiceConfig()
        {
            LogLevel = LogSeverity.Debug,
            CaseSensitiveCommands = false,
            IgnoreExtraArgs = true,
            ThrowOnError = false
        };

        services.AddSingleton(socketConfig)
            .AddSingleton(interactionServiceConfig)
            .AddSingleton(commandServiceConfig)
            .AddSingleton<IRestClientProvider>(x => x.GetRequiredService<DiscordShardedClient>())
            .AddSingleton<DiscordShardedClient>()
            .AddSingleton<InteractionService>()
            .AddSingleton<CommandService>();

        return services;
    }

    /// <summary>
    /// Adds all Discord event handlers to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddDiscordEventHandlers(this IServiceCollection services)
    {
        // Event Handlers
        services.AddSingleton<OnLogMessage>()
            .AddSingleton<OnMessage>()
            .AddSingleton<OnInteraction>()
            .AddSingleton<OnInteractionExecuted>()
            .AddSingleton<OnShardReady>()
            .AddSingleton<OnCommandExecuted>();

        return services;
    }
}
