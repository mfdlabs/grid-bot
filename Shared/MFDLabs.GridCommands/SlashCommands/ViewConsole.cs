#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.FileSystem;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;

using System.Runtime.InteropServices;
using MFDLabs.Drawing;
using MFDLabs.Threading;
using MFDLabs.Networking;
using MFDLabs.Grid.Bot.Utility;

using HWND = System.IntPtr;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal class ViewConsole : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "View Grid Server Console";
        public string CommandAlias => "viewconsole";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;

        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("command_id")
                .WithDescription("Screenshot a grid server based on a slash command that performed a script execution.")
                .WithType(ApplicationCommandOptionType.Integer)
                .WithRequired(true),

            new SlashCommandOptionBuilder()
                .WithName("show_recent_executions")
                .WithDescription("Displays an embed of all recent executions within that channel.")
                .WithType(ApplicationCommandOptionType.SubCommand)
        };

        private sealed class ViewConsoleCommandPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.SlashCommands.ViewConsole";

            public IRawValueCounter TotalItemsProcessed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatFailed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
            public IAverageValueCounter ViewConsoleSlashCommandSuccessAverageTimeTicks { get; }
            public IAverageValueCounter ViewConsoleSlashCommandFailureAverageTimeTicks { get; }

            public ViewConsoleCommandPerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
                TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedPerSecond", instance);
                TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
                TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatFailedPerSecond", instance);
                ViewConsoleSlashCommandSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ViewConsoleSlashCommandSuccessAverageTimeTicks", instance);
                ViewConsoleSlashCommandFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ViewConsoleSlashCommandFailureAverageTimeTicks", instance);
            }
        }

        #region Metrics

        private static readonly ViewConsoleCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

        private static void MaximizeGridServer([In] HWND hWnd)
        {
            const int SW_MAXIMIZE = 3;

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool ShowWindow(HWND hWnd, int nCmdShow);

            ShowWindow(hWnd, SW_MAXIMIZE);
        }

        private static async Task ScreenshotSingleGridServerAndRespond(SocketSlashCommand command)
        {
            var fileName = $"{NetworkingGlobal.GenerateUuidv4()}.png";
            var tempPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp", fileName);
            try
            {
                var mainWindowHandle = ProcessHelper.GetWindowHandle("rccservice");
                MaximizeGridServer(mainWindowHandle);
                var bitMap = mainWindowHandle.GetBitmapForWindowByWindowHandle();
                bitMap.Save(tempPath);
                var stream = new MemoryStream(File.ReadAllBytes(tempPath));

                await command.RespondWithFilePublicPingAsync(stream, fileName, "Grid Server Output:");
            }
            finally
            {
                tempPath.PollDeletion();
            }
        }

        private static async Task ProcessSingleInstancedGridServerScreenshot(SocketSlashCommand command)
        {
            var tte = GridProcessHelper.OpenServerSafe().elapsed;

            if (tte.TotalSeconds > 1.5)
            {
                // Wait for 1.25s so the grid server output can be populated.
                TaskHelper.SetTimeoutFromMilliseconds(() => ScreenshotSingleGridServerAndRespond(command).Wait(), 1250);
                return;
            }

            await ScreenshotSingleGridServerAndRespond(command);
        }

        public async Task Invoke(SocketSlashCommand command)
        {
            _perfmon.TotalItemsProcessed.Increment();
            _perfmon.TotalItemsProcessedPerSecond.Increment();

            var sw = Stopwatch.StartNew();
            bool failure = false;

            try
            {
                if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                {
                    await ProcessSingleInstancedGridServerScreenshot(command);
                    return;
                }

                if (command.Data.GetSubCommand() != null)
                {
                    var embed = command.ConstructUserLookupEmbed();
                    if (embed == null)
                    {
                        await command.RespondEphemeralPingAsync("You haven't executed any scripts in this channel!");
                        return;
                    }

                    await command.RespondEphemeralPingAsync(
                        "Type `/viewconsole {slashCommandId}` or reply to the message to screenshot the console of the message.",
                        embed: embed
                    );
                    return;
                }

                var slashCommandId = command.Data.GetOptionValue("command_id")?.ToUInt64();
                var (stream, fileName, status, _) = command.ScreenshotGridServer(slashCommandId.Value);

                switch (status)
                {
                    case GridServerArbiterScreenshotUtilityV2.ScreenshotStatus.NoRecentExecutions:
                        await command.RespondEphemeralPingAsync("You haven't executed any scripts in this channel!");
                        break;
                    case GridServerArbiterScreenshotUtilityV2.ScreenshotStatus.UnknownSlashCommandId:
                    case GridServerArbiterScreenshotUtilityV2.ScreenshotStatus.NullInstance:
                        await command.RespondEphemeralPingAsync($"There was no script execution found with the slash command id '{slashCommandId}', " +
                                                                $"re run the command with no arguments to see what messages you contain scripts.");
                        break;

                    case GridServerArbiterScreenshotUtilityV2.ScreenshotStatus.Success:
                        await command.RespondWithFilePublicPingAsync(stream, fileName);
                        break;

                    case GridServerArbiterScreenshotUtilityV2.ScreenshotStatus.DisposedInstance:
                    default:
                        await command.RespondEphemeralPingAsync("Internal Exception."); // for now, will figure out actual message later.
                        break;
                }
            }
            finally
            {
                sw.Stop();
                Logger.Singleton.Debug("Took {0}s to execute view console command.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (failure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.ViewConsoleSlashCommandFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
                else
                {
                    _perfmon.ViewConsoleSlashCommandSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }
    }
}

#endif