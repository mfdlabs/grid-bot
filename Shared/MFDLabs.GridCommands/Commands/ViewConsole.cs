using System;
using System.IO;
using System.Text;
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
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;


#if NETFRAMEWORK
using System.Linq;
using System.Runtime.InteropServices;
using MFDLabs.Drawing;
using MFDLabs.Threading;
using MFDLabs.Networking;
using MFDLabs.Grid.Bot.Utility;

using HWND = System.IntPtr;
#endif

namespace MFDLabs.Grid.Bot.Commands
{
    internal class ViewConsole : IStateSpecificCommandHandler
    {
        public string CommandName => "View Grid Server Console";
        public string CommandDescription => "Dispatches a 'ScreenshotTask' request to the task thread port." +
                                            " Will try to screenshot the current grid server's console output.";
        public string[] CommandAliases => new[] { "vc", "viewconsole" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        private sealed class ViewConsoleCommandPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.Commands.ViewConsole";

            public IRawValueCounter TotalItemsProcessed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatFailed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
            public IAverageValueCounter ViewConsoleCommandSuccessAverageTimeTicks { get; }
            public IAverageValueCounter ViewConsoleCommandFailureAverageTimeTicks { get; }

            public ViewConsoleCommandPerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
                TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedPerSecond", instance);
                TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
                TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatFailedPerSecond", instance);
                ViewConsoleCommandSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ViewConsoleCommandSuccessAverageTimeTicks", instance);
                ViewConsoleCommandFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "ViewConsoleCommandFailureAverageTimeTicks", instance);
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

        private static async Task ScreenshotSingleGridServerAndRespond(SocketMessage message)
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

                await message.ReplyWithFileAsync(stream, fileName, "Grid Server Output:");
            }
            finally
            {
                tempPath.PollDeletion();
            }
        }

        private static async Task ProcessSingleInstancedGridServerScreenshot(SocketMessage message)
        {
            using (message.Channel.EnterTypingState())
            {
                var tte = GridProcessHelper.OpenServerSafe().elapsed;

                if (tte.TotalSeconds > 1.5)
                {
                    // Wait for 1.25s so the grid server output can be populated.
                    TaskHelper.SetTimeoutFromMilliseconds(() => ScreenshotSingleGridServerAndRespond(message).Wait(), 1250);
                    return;
                }

                await ScreenshotSingleGridServerAndRespond(message);
            }
        }

        public async Task Invoke(string[] contentArray, SocketMessage message, string originalCommand)
        {
            _perfmon.TotalItemsProcessed.Increment();
            _perfmon.TotalItemsProcessedPerSecond.Increment();

            var sw = Stopwatch.StartNew();
            bool failure = false;

            try
            {
                if (global::MFDLabs.Grid.Properties.Settings.Default.SingleInstancedGridServer)
                {
                    await ProcessSingleInstancedGridServerScreenshot(message);
                    return;
                }

                if (!contentArray.Any() && message.Reference == null)
                {
                    var embed = message.ConstructUserLookupEmbed();
                    if (embed == null)
                    {
                        await message.ReplyAsync("You haven't executed any scripts in this channel!");
                        return;
                    }

                    await message.ReplyAsync(
                        $"Type `{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}viewconsole {{messageId}}` or reply to the message to screenshot the console of the message.",
                        embed: embed
                    );
                    return;
                }

                var messageId = 0ul;

                if (message.Reference is not null)
                {
                    messageId = message.Reference.MessageId.Value;
                }
                else
                {
                    var clientIdx = contentArray.First();

                    if (!ulong.TryParse(clientIdx, out messageId))
                    {
                        failure = true;
                        message.Reply($"The first argument of '{contentArray.First()}' was not a valid message id.");
                        return;
                    }
                }

                var (stream, fileName, status, _) = message.ScreenshotGridServer(messageId);

                switch (status)
                {
                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.NoRecentExecutions:
                        message.Reply("You haven't executed any scripts in this channel!");
                        break;
                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.UnknownMessageId:
                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.NullInstance:
                        message.Reply($"There was no script execution found with the message id '{messageId}', " +
                                      $"re run the command with no arguments to see what messages you contain scripts.");
                        break;

                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.Success:
                        message.ReplyWithFile(stream, fileName);
                        break;

                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.DisposedInstance:
                    default:
                        message.Reply("Internal Exception."); // for now, will figure out actual message later.
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
                    _perfmon.ViewConsoleCommandFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
                else
                {
                    _perfmon.ViewConsoleCommandSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }
    }
}
