using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Logging;

using Networking;
using Diagnostics;
using Text.Extensions;
using Grid.Bot.Events;
using Grid.Bot.Global;
using Grid.Bot.Utility;
using Grid.Bot.Properties;
using Grid.Bot.Registries;
using Configuration.Extensions;
using Grid.Bot.PerformanceMonitors;

namespace Grid.Bot
{
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

        public static void OnGlobalException(Exception ex)
        {
            Logger.Singleton.Error(PrimaryTaskError);

            PerformanceServer.Stop();
        }

        public static void Invoke(string[] args)
        {
            Logger.Singleton.Warning(BadActorMessage);

#if DEBUG
            if (global::Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutDebugMode)
                Logger.Singleton.Warning(DebugMode);
#endif

            if (SystemGlobal.ContextIsAdministrator() &&
                global::Grid.Bot.Properties.Settings.Default.OnLaunchWarnAboutAdminMode)
                Logger.Singleton.Warning(AdminMode);

            if (args.Contains("--write-settings"))
            {
                Logger.Singleton.Warning("Writing settings instead of actually launching.");

                global::Grid.Bot.Properties.Settings.Default.Save();

                Environment.Exit(0);
                return;
            }

            Logger.Singleton.Debug(
                "Process '{0}' opened with file name '{1}' at path '{2}' (version {3}).",
                SystemGlobal.CurrentProcess.Id.ToString("x"),
                SystemGlobal.CurrentProcess.ProcessName,
                Directory.GetCurrentDirectory(),
                SystemGlobal.AssemblyVersion
            );

            Console.Title = string.Format(Resources.Runner_Invoke_Title,
                SystemGlobal.CurrentProcess.Id,
                SystemGlobal.CurrentProcess.ProcessName,
                SystemGlobal.AssemblyVersion,
                NetworkingGlobal.GetLocalIp(),
                SystemGlobal.GetMachineHost(),
                SystemGlobal.GetMachineId()
            );

            if (global::Grid.Bot.Properties.Settings.Default.ShouldLaunchCounterServer)
                PerformanceServer.Start();

            InvokeAsync(args).Wait();

            Environment.Exit(0);
        }

        private static async Task InvokeAsync(IEnumerable<string> args)
        {
            // For Unix, skip this, as I assume we won't need this:)
            ConsoleHookRegistry.Register();

            if (global::Grid.Bot.Properties.Settings.Default.BotToken.FromEnvironmentExpression<string>().IsNullOrWhiteSpace())
            {
                Logger.Singleton.Error(NoBotToken);
                // Case here so backtrace can catch potential hackers trying to use this without a token
                // (they got assemblies but no configuration)
                throw new InvalidOperationException(NoBotToken);
            }

            BotRegistry.Initialize(
#if DISCORD_SHARDING_ENABLED
                new DiscordShardedClient(
#else
                new DiscordSocketClient(
#endif
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
                )
            );

            BotRegistry.Client.Log += OnLogMessage.Invoke;
            BotRegistry.Client.MessageReceived += OnMessage.Invoke;

#if DISCORD_SHARDING_ENABLED
            BotRegistry.Client.ShardReady += OnShardReady.Invoke;
#else
            BotRegistry.Client.Ready += OnReady.Invoke;
#endif

#if WE_LOVE_EM_SLASH_COMMANDS
            BotRegistry.Client.SlashCommandExecuted += OnSlashCommand.Invoke;
#endif // WE_LOVE_EM_SLASH_COMMANDS

            var defaultHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.None)
            {
                MaxReceivedMessageSize = int.MaxValue,
                SendTimeout = global::Grid.Bot.Properties.Settings.Default.GridServerArbiterDefaultTimeout
            };

            GridServerArbiter.SetDefaultHttpBinding(defaultHttpBinding);
            GridServerArbiter.SetCounterRegistry(PerfmonCounterRegistryProvider.Registry);

            if (global::Grid.Bot.Properties.Settings.Default.GridServerArbiterQueueUpEnabled)
                GridServerArbiter.Singleton.BatchCreateLeasedInstances(
                    count: 25
                );

            if (global::Grid.Bot.Properties.Settings.Default.OnStartCloseAllOpenGridServerInstances)
                GridServerArbiter.Singleton.KillAllInstances();

            Task.Run(ShutdownUdpReceiver.Receive);

            if (!args.Contains("--no-gateway"))
                await BotRegistry.SingletonLaunch();

            await Task.Delay(-1);
        }
    }
}
