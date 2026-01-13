namespace Grid.Bot;

using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Prometheus;

using Logging;
using Configuration;

using Web;
using Grpc;
using Utility;
using Extensions;

using Logger = Logging.Logger;
using ILogger = Logging.ILogger;

internal static class Runner
{
#if DEBUG
    private const string DebugMode = "WARNING: RUNNING IN DEBUG MODE, THIS CAN POTENTIALLY LEAK MORE INFORMATION " +
                                     "THAN NEEDED, PLEASE RUN THIS ON RELEASE FOR PRODUCTION SCENARIOS.";
#endif

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

    private static ServiceProvider InitializeServices()
    {
        var services = new ServiceCollection();

        services.AddSettingsProviders();
        services.AddGlobalLogger();
        services.AddUtilities();

        services.AddJobManager();
        services.AddFloodCheckersRedis();
        services.AddHttpClients();
        services.AddClientSettings();

        services.AddDiscord();
        services.AddDiscordEventHandlers();

        services.AddGrpc();

        return services.BuildServiceProvider();
    }

    private static void LogStartupInfo(ILogger logger)
    {
        var informationalVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var metadataAttributes = Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>();
        var assemblyMetadataAttributes = metadataAttributes as AssemblyMetadataAttribute[] ?? metadataAttributes.ToArray();
        
        var buildTimeStamp = DateTime.Parse(assemblyMetadataAttributes.FirstOrDefault(a => a.Key == "BuildTimestamp")?.Value ?? "1/1/1970");
        var gitHash = assemblyMetadataAttributes.FirstOrDefault(a => a.Key == "GitHash")?.Value ?? "Unknown";
        var gitBranch = assemblyMetadataAttributes.FirstOrDefault(a => a.Key == "GitBranch")?.Value ?? "Unknown";

        logger.Information($"Starting Grid.Bot, Version = {informationalVersion}, BuildTimeStamp = {buildTimeStamp}, GitHash = {gitHash}, GitBranch = {gitBranch}");
    
        Metrics.CreateGauge(
            "grid_bot_build_info",
            "Grid.Bot build information",
            "version", "git_hash", "git_branch", "build_timestamp"
        ).WithLabels(informationalVersion ?? "unk", gitHash, gitBranch, buildTimeStamp.ToString("o")).Set(1);
    }

    private static async Task InvokeAsync(string[] args)
    {
        if (args.Contains("--write-local-config"))
        {
            var providers = ServiceCollectionExtensions.GetSettingsProviders();

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
        LogStartupInfo(logger);

#if DEBUG
        logger.Warning(DebugMode);
#endif

        services.UploadAllLogFilesToBacktrace();

        services.UseGrpcServer(args);
        services.UseWebServer(args);
        services.UseMetricsServer();

        await services.UseDiscordAsync().ConfigureAwait(false);

        await Task.Delay(Timeout.Infinite).ConfigureAwait(false);
    }
}
