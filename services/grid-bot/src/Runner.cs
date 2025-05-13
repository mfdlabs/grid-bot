namespace Grid.Bot;

using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.Rest;
using Discord.WebSocket;

using Discord.Commands;
using Discord.Interactions;

using Vault;
using Redis;
using Random;
using Logging;
using Networking;
using Configuration;
using Text.Extensions;
using ServiceDiscovery;

using Users.Client;
using Thumbnails.Client;

using Web;
using Grpc;
using Events;
using Utility;
using Prometheus;

using Logger = Logging.Logger;
using ILogger = Logging.ILogger;

using LoggerFactory = Utility.LoggerFactory;
using ILoggerFactory = Utility.ILoggerFactory;

internal static class Runner
{
#if DEBUG
    private const string _debugMode = "WARNING: RUNNING IN DEBUG MODE, THIS CAN POTENTIALLY LEAK MORE INFORMATION " +
                                     "THAN NEEDED, PLEASE RUN THIS ON RELEASE FOR PRODUCTION SCENARIOS.";
#endif
    private const string _noBotToken = "The setting \"BotToken\" was null when it is required.";

    private static IServiceProvider _services;

    public static void Invoke(string[] args)
    {
        InvokeAsync(args).Wait();

        Environment.Exit(0);
    }

    internal static void ReportError(Exception ex)
    {
        var backtraceUtility = _services?.GetService<IBacktraceUtility>();

        backtraceUtility?.UploadException(ex);
    }

    private static void CollectLogsAndReportToBacktrace(ILogger logger, BacktraceSettings backtraceSettings, IBacktraceUtility utility, IPercentageInvoker percentageInvoker)
    {
        if (utility == null) return;

        try
        {
            percentageInvoker.InvokeAction(
                () => utility.UploadAllLogFiles(true),
                backtraceSettings.UploadLogFilesToBacktraceEnabledPercent
            );
        }
        catch (Exception ex)
        {
            logger.Warning($"Log file upload to Backtrace failed: {ex}");
        }
    }

    private static ServiceProvider InitializeServices()
    {
        var services = new ServiceCollection();

        // Add each individual provider, iterate through the via Reflection
        // Assembly is Shared.Settings
        var providers = GetSettingsProviders();

        foreach (var singleton in providers)
        {
            if (singleton == null) continue;

            services.AddSingleton(singleton.GetType(), singleton);

            // If they implement interfaces, add those too.
            foreach (var iface in singleton.GetType().GetInterfaces())
                services.AddSingleton(iface, singleton);
        }

        var globalSettings = providers.FirstOrDefault(s => s.GetType() == typeof(GlobalSettings)) as GlobalSettings;

        var logger = new Logger(
            name: globalSettings.DefaultLoggerName,
            logLevelGetter: () => globalSettings.DefaultLoggerLevel,
            logToConsole: globalSettings.DefaultLoggerLogToConsole
        );

        services.AddSingleton<ILogger>(logger);

        var informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        var metadataAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>();

        var buildTimeStamp = DateTime.Parse(metadataAttributes.FirstOrDefault(a => a.Key == "BuildTimestamp")?.Value ?? "1/1/1970");
        var gitHash = metadataAttributes.FirstOrDefault(a => a.Key == "GitHash")?.Value ?? "Unknown";
        var gitBranch = metadataAttributes.FirstOrDefault(a => a.Key == "GitBranch")?.Value ?? "Unknown";

        logger.Information($"Starting Grid.Bot, Version = {informationalVersion}, BuildTimeStamp = {buildTimeStamp}, GitHash = {gitHash}, GitBranch = {gitBranch}");

        var config = new DiscordSocketConfig()
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

        var gridSettings = providers.FirstOrDefault(s => s.GetType() == typeof(GridSettings)) as GridSettings;
        SetupJobManager(services, gridSettings);

        var floodCheckerSettings = providers.FirstOrDefault(s => s.GetType() == typeof(FloodCheckerSettings)) as FloodCheckerSettings;
        var consulSettings = providers.FirstOrDefault(s => s.GetType() == typeof(ConsulSettings)) as ConsulSettings;
        SetupFloodCheckersRedis(services, floodCheckerSettings, consulSettings, logger);

        var usersClientSettings = providers.FirstOrDefault(s => s.GetType() == typeof(UsersClientSettings)) as UsersClientSettings;
        var usersClient = new UsersClient(usersClientSettings.UsersApiBaseUrl);
        services.AddSingleton<IUsersClient>(usersClient);

        var webSettings = providers.FirstOrDefault(s => s.GetType() == typeof(WebSettings)) as WebSettings;
        var clientSettingsSettings = providers.FirstOrDefault(s => s.GetType() == typeof(ClientSettingsSettings)) as ClientSettingsSettings;
        var vaultClient = clientSettingsSettings.ClientSettingsViaVault
            ? VaultClientFactory.Singleton.GetClient(clientSettingsSettings.ClientSettingsVaultAddress, clientSettingsSettings.ClientSettingsVaultToken)
            : null;
        var clientSettingsFactory = new ClientSettingsFactory(
            vaultClient,
            logger,
            clientSettingsSettings
        );
        services.AddSingleton<IClientSettingsFactory>(clientSettingsFactory);

        var avatarSettings = providers.FirstOrDefault(s => s.GetType() == typeof(AvatarSettings)) as AvatarSettings;
        var thumbnailsClient = new ThumbnailsClient(avatarSettings.RbxThumbnailsUrl);
        services.AddSingleton<IThumbnailsClient>(thumbnailsClient);

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
            .AddSingleton<IGridServerFileHelper, GridServerFileHelper>();

        services.AddSingleton(config)
            .AddSingleton(interactionServiceConfig)
            .AddSingleton<IRestClientProvider>(x => x.GetRequiredService<DiscordShardedClient>())
            .AddSingleton<DiscordShardedClient>()
            .AddSingleton<InteractionService>()
            .AddSingleton<CommandService>();

        // Event Handlers
        services.AddSingleton<OnLogMessage>()
            .AddSingleton<OnMessage>()
            .AddSingleton<OnInteraction>()
            .AddSingleton<OnInteractionExecuted>()
            .AddSingleton<OnShardReady>()
            .AddSingleton<OnCommandExecuted>();

        // Http Client Factory
        services.AddHttpClient();

        services.AddGrpc();

        return services.BuildServiceProvider();
    }

    private static IEnumerable<IConfigurationProvider> GetSettingsProviders()
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

    private static void SetupJobManager(ServiceCollection services, GridSettings gridSettings)
    {
#if DEBUG
        if (gridSettings.DebugUseNoopJobManager)
        {
            services.AddSingleton<IJobManager, NoopJobManager>();

            return;
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
    }

    private static void SetupFloodCheckersRedis(ServiceCollection services, FloodCheckerSettings floodCheckerSettings, ConsulSettings consulSettings, ILogger logger)
    {
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
    }

    private static async Task InvokeAsync(IEnumerable<string> args)
    {

        if (args.Contains("--write-local-config"))
        {
            var providers = GetSettingsProviders();

            Logger.Singleton.LogLevel = LogLevel.Verbose;
            Logger.Singleton.Information("Applying local configuration to Vault and exiting!");

            foreach (var provider in providers.Cast<IVaultProvider>())
            {
                provider.SetLogger(Logger.Singleton);

                provider.ApplyCurrent();
            }

            Console.ReadKey();
            return;
        }

        var services = InitializeServices();

        _services = services;

        var logger = services.GetRequiredService<ILogger>();

#if DEBUG
        logger.Warning(_debugMode);
#endif

        var backtraceSettings = services.GetRequiredService<BacktraceSettings>();
        var backtraceUtility = services.GetRequiredService<IBacktraceUtility>();
        var percentageInvoker = services.GetRequiredService<IPercentageInvoker>();

        CollectLogsAndReportToBacktrace(logger, backtraceSettings, backtraceUtility, percentageInvoker);

        var discordSettings = services.GetRequiredService<DiscordSettings>();

#if DEBUG
        if (discordSettings.BotToken.IsNullOrEmpty() && !discordSettings.DebugBotDisabled)
#else
        if (discordSettings.BotToken.IsNullOrEmpty())
#endif
        {
            logger.Error(_noBotToken);

            // Case here so backtrace can catch potential hackers trying to use this without a token
            // (they got assemblies but no configuration)
            throw new InvalidOperationException(_noBotToken);
        }

        services.UseGrpcServer(args);
        services.UseWebServer(args);

        var client = services.GetRequiredService<DiscordShardedClient>();
        var interactions = services.GetRequiredService<InteractionService>();
        var commands = services.GetRequiredService<CommandService>();

        var onLogMessage = services.GetRequiredService<OnLogMessage>();
        var onShardReady = services.GetRequiredService<OnShardReady>();

        client.Log += onLogMessage.Invoke;
        interactions.Log += onLogMessage.Invoke;
        commands.Log += onLogMessage.Invoke;

        client.ShardReady += onShardReady.Invoke;

        var globalSettings = services.GetRequiredService<GlobalSettings>();

        new KestrelMetricServer(
            port: globalSettings.MetricsPort
        ).Start();

#if DEBUG
        if (!discordSettings.DebugBotDisabled)
#endif
        {
            await client.LoginAsync(TokenType.Bot, discordSettings.BotToken).ConfigureAwait(false);
            await client.StartAsync().ConfigureAwait(false);
        }

        await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
    }
}
