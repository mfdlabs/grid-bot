namespace Grid.Bot;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Logging;

using Random;
using Networking;
using Configuration;
using Text.Extensions;
using Instrumentation;

using Events;
using Global;
using Utility;

internal static class Runner
{
#if DEBUG
    private const string DebugMode = "WARNING: RUNNING IN DEBUG MODE, THIS CAN POTENTIALLY LEAK MORE INFORMATION " +
                                     "THAN NEEDED, PLEASE RUN THIS ON RELEASE FOR PRODUCTION SCENARIOS.";
#endif
    private const string AdminMode = "WARNING: RUNNING AS ADMINSTRATOR, THIS CAN POTENTIALLY BE DANGEROUS " +
                                     "SECURITY WISE, PLEASE KNOW WHAT YOU ARE DOING!";
    private const string PrimaryTaskError = "An exception occurred when trying to execute the primary task, please check back trace!";
    private const string NoBotToken = "The setting \"BotToken\" was null when it is required.";
    private const string BadActorMessage = "THIS SOFTWARE IS UNLICENSED, IF YOU DO NOT HAVE EXPLICIT WRITTEN PERMISSION " +
                                           "BY THE CONTRIBUTORS OR THE PRIMARY DEVELOPER TO USE THIS, DELETE IT IMMEDIATELY!";

    public static void OnGlobalException()
    {
        Logger.Singleton.Error(PrimaryTaskError);
    }

    public static void Invoke(string[] args)
    {
#if USE_VAULT_SETTINGS_PROVIDER

        if (args.Contains("--write-local-config"))
        {
            ConfigurationProvider.SetUp(false);

            Logger.Singleton.Information("Applying local configuration to Vault and exiting!");

            foreach (var provider in ConfigurationProvider.RegisteredProviders.Cast<IVaultProvider>())
                provider.ApplyCurrent();

            Console.ReadKey();
            return;
        }

        ConfigurationProvider.SetUp();
#endif

        Logger.Singleton.Warning(BadActorMessage);

#if DEBUG
        Logger.Singleton.Warning(DebugMode);
#endif

        var isAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        if (isAdministrator)
            Logger.Singleton.Warning(AdminMode);

        Task.Factory.StartNew(CollectLogsAndReportToBacktrace);

        var currentProcess = Process.GetCurrentProcess();
        var currentAssemblyVersion = Assembly.GetExecutingAssembly()?.GetName().Version?.ToString();

        Logger.Singleton.Debug(
            "Process '{0}' opened with file name '{1}' at path '{2}' (version {3}).",
            currentProcess.Id.ToString("x"),
            currentProcess.ProcessName,
            Directory.GetCurrentDirectory(),
            currentAssemblyVersion
        );

        Console.Title = string.Format(Resources.Runner_Invoke_Title,
            currentProcess.Id,
            currentProcess.ProcessName,
            currentAssemblyVersion,
            LocalIpAddressProvider.Singleton.AddressV4,
            LocalIpAddressProvider.Singleton.GetHostName(),
            Environment.MachineName
        );

        InvokeAsync(args).Wait();

        Environment.Exit(0);
    }

    private static void CollectLogsAndReportToBacktrace()
    {
        PercentageInvoker.Singleton.InvokeAction(
            () => BacktraceUtility.UploadAllLogFiles(true, false),
            BacktraceSettings.Singleton.UploadLogFilesToBacktraceEnabledPercent
        );
    }

    private static async Task InvokeAsync(IEnumerable<string> args)
    {
        if (DiscordSettings.Singleton.BotToken.IsNullOrEmpty())
        {
            Logger.Singleton.Error(NoBotToken);
            // Case here so backtrace can catch potential hackers trying to use this without a token
            // (they got assemblies but no configuration)
            throw new InvalidOperationException(NoBotToken);
        }

        BotRegistry.Client = new DiscordShardedClient(
            new DiscordSocketConfig
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
            }
        );

        BotRegistry.Client.Log += OnLogMessage.Invoke;
        BotRegistry.Client.MessageReceived += OnMessage.Invoke;

        BotRegistry.Client.ShardReady += OnShardReady.Invoke;

#if WE_LOVE_EM_SLASH_COMMANDS
        BotRegistry.Client.SlashCommandExecuted += OnSlashCommand.Invoke;
#endif // WE_LOVE_EM_SLASH_COMMANDS

        var defaultHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.None)
        {
            MaxReceivedMessageSize = int.MaxValue,
            SendTimeout = ArbiterSettings.Singleton.GridServerArbiterDefaultTimeout
        };

        GridServerArbiter.SetDefaultHttpBinding(defaultHttpBinding);
        GridServerArbiter.SetDefaultCounterRegistry(StaticCounterRegistry.Instance);
        GridServerArbiter.SetDefaultSettings(ArbiterSettings.Singleton);

        Task.Factory.StartNew(AutoDeployerUpgradeReciever.Receive);

        FloodCheckersRedisClientProvider.SetUp();

        if (!args.Contains("--no-gateway"))
        {
            await BotRegistry.Client.LoginAsync(TokenType.Bot, DiscordSettings.Singleton.BotToken).ConfigureAwait(false);

            await BotRegistry.Client.StartAsync().ConfigureAwait(false);
        }

        await Task.Delay(-1);
    }
}
