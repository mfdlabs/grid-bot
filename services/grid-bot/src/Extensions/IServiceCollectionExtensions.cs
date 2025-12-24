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

using Microsoft.Extensions.DependencyInjection;

using Vault;
using Redis;
using Random;
using Logging;
using Networking;
using Configuration;
using ServiceDiscovery;

using Users.Client;
using Thumbnails.Client;

using Events;
using Utility;

using EnvironmentProvider = Grid.Bot.EnvironmentProvider;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Get all settings providers in the assembly.
    /// </summary>
    /// <returns>The <see cref="IConfigurationProvider"/>s.</returns>
    internal static IEnumerable<IConfigurationProvider> GetSettingsProviders()
    {
        var assembly = Assembly.GetAssembly(typeof(BaseSettingsProvider));
        var @namespace = typeof(BaseSettingsProvider).Namespace;

        var types = assembly
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, @namespace, StringComparison.Ordinal) &&
                        t.BaseType.Name == typeof(BaseSettingsProvider).Name)
            .ToList(); // finicky

        var singletons = new List<IConfigurationProvider>();

        foreach (var t in types)
        {
            // Construct the singleton.
            var constructor = t.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
            {
                Console.Error.WriteLine("Provider {0} did not expose a public constructor!", t.FullName);

                singletons.Add(null);

                continue;
            }

            var singleton = constructor.Invoke(null);
            if (singleton is not IConfigurationProvider provider)
            {
                Console.Error.WriteLine("Provider {0} did not construct a singleton!", t.FullName);

                singletons.Add(null);

                continue;
            }

            singletons.Add(provider);
        }

        return singletons.Cast<IConfigurationProvider>();
    }
    
    /// <summary>
    /// Add settings classes and their interfaces to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddSettingsProviders(this IServiceCollection services)
    {
        var providers = GetSettingsProviders();

        foreach (var singleton in providers)
        {
            if (singleton == null) continue;

            services.AddSingleton(singleton.GetType(), singleton);

            // If they implement interfaces, add those too.
            foreach (var iface in singleton.GetType().GetInterfaces())
                services.AddSingleton(iface, singleton);
        }

        return services;
    }

    /// <summary>
    /// Add all utilities to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddUtilities(this IServiceCollection services)
    {
        services.AddSingleton<IBacktraceUtility, BacktraceUtility>()
            .AddSingleton<IAdminUtility, AdminUtility>()
            .AddSingleton<IAvatarUtility, AvatarUtility>()
            .AddSingleton<ILuaUtility, LuaUtility>()
            .AddSingleton<IRbxUsersUtility, RbxUsersUtility>()
            .AddSingleton<IDiscordWebhookAlertManager, DiscordWebhookAlertManager>()
            .AddSingleton<IScriptLogger, ScriptLogger>()
            .AddSingleton<IPercentageInvoker, PercentageInvoker>()
            .AddSingleton<IRandom>(RandomFactory.GetDefaultRandom())
            .AddSingleton<ILoggerFactory, LoggerFactory>()
            .AddSingleton<ILocalIpAddressProvider, LocalIpAddressProvider>()
            .AddSingleton<IGridServerFileHelper, GridServerFileHelper>()
            .AddSingleton<IVaultClientFactory, VaultClientFactory>();

        return services;
    }

    /// <summary>
    /// Add the global logger to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddGlobalLogger(this IServiceCollection services)
    {
        var globalSettings = services
            .BuildServiceProvider()
            .GetRequiredService<GlobalSettings>();

        var logger = new Logger(
            name: globalSettings.DefaultLoggerName,
            logLevelGetter: () => globalSettings.DefaultLoggerLevel,
            logToConsole: globalSettings.DefaultLoggerLogToConsole
        );

        services.AddSingleton<ILogger>(logger);

        return services;
    }

    /// <summary>
    /// Adds the job manager to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddJobManager(this IServiceCollection services)
    {
        var gridSettings = services
            .BuildServiceProvider()
            .GetRequiredService<GridSettings>();

#if DEBUG
        if (gridSettings.DebugUseNoopJobManager)
        {
            services.AddSingleton<IJobManager, NoopJobManager>();

            return services;
        }
#endif

        var logger = new Logger(
            name: gridSettings.JobManagerLoggerName,
            logLevelGetter: () => gridSettings.JobManagerLogLevel,
            logToConsole: gridSettings.JobManagerLogToConsole
        );

        var portAllocator = new PortAllocator(logger);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var jobManager = new ProcessJobManager(
                logger,
                portAllocator,
                gridSettings
            );

            jobManager.Start();

            services.AddSingleton(jobManager);
            services.AddSingleton(_ => null as DockerJobManager);

        }
        else
        {
            var jobManager = new DockerJobManager(
                logger,
                portAllocator,
                gridSettings,
                RandomFactory.GetDefaultRandom()
            );

            jobManager.Start();

            services.AddSingleton(jobManager);
            services.AddSingleton(_ => null as ProcessJobManager);
        }

        services.AddSingleton<IJobManager, JobManager>();

        return services;
    }

    /// <summary>
    /// Adds the floodcheckers Redis client to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddFloodCheckersRedis(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        var floodCheckerSettings = serviceProvider.GetRequiredService<FloodCheckerSettings>();
        var consulSettings = serviceProvider.GetRequiredService<ConsulSettings>();
        var logger = serviceProvider.GetRequiredService<ILogger>();

        var consulClientProvider = new LocalConsulClientProvider(consulSettings);
        var serviceResolver = new ConsulHttpServiceResolver(
            consulSettings,
            logger,
            consulClientProvider,
            floodCheckerSettings.ToSingleSetting(s => s.FloodCheckersConsulServiceName),
            EnvironmentProvider.EnvironmentName,
            floodCheckerSettings.FloodCheckersRedisUseServiceDiscovery
        );

        var redisClient = new HybridRedisClientProvider(
            floodCheckerSettings,
            logger,
            serviceResolver,
            floodCheckerSettings.ToSingleSetting(s => s.FloodCheckersRedisUseServiceDiscovery),
            floodCheckerSettings.ToSingleSetting(s => s.FloodCheckersRedisEndpoints)
        ).Client;

        var floodCheckerRegistry = new FloodCheckerRegistry(
            logger,
            redisClient,
            floodCheckerSettings
        );

        services.AddSingleton<IFloodCheckerRegistry>(floodCheckerRegistry);

        return services;
    }

    /// <summary>
    /// Adds the specific HTTP clients to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddHttpClients(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        var usersClientSettings = serviceProvider.GetRequiredService<UsersClientSettings>();
        var avatarSettings = serviceProvider.GetRequiredService<AvatarSettings>();

        var usersClient = new UsersClient(usersClientSettings.UsersApiBaseUrl);
        services.AddSingleton<IUsersClient>(usersClient);

        var thumbnailsClient = new ThumbnailsClient(avatarSettings.RbxThumbnailsUrl);
        services.AddSingleton<IThumbnailsClient>(thumbnailsClient);

        services.AddHttpClient();

        return services;
    }

    /// <summary>
    /// Adds all client settings related components to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddClientSettings(this IServiceCollection services)
    {
        var serviceProvider = services.BuildServiceProvider();

        var logger = serviceProvider.GetRequiredService<ILogger>();
        var clientSettingsSettings = serviceProvider.GetRequiredService<ClientSettingsSettings>();
        var vaultClientFactory = serviceProvider.GetRequiredService<IVaultClientFactory>();

        var vaultClient = clientSettingsSettings.ClientSettingsViaVault
            ? vaultClientFactory.GetClient(clientSettingsSettings.ClientSettingsVaultAddress,
                                         clientSettingsSettings.ClientSettingsVaultToken)
            : null;

        var clientSettingsFactory = new ClientSettingsFactory(
            vaultClient,
            logger,
            clientSettingsSettings
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