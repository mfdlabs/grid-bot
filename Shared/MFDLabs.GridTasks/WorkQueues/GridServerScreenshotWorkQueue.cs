using System;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Ccr.Core;
using Discord;
using MFDLabs.Concurrency;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Instrumentation;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Logging;
using System.IO;
using MFDLabs.Diagnostics;
using System.Diagnostics;


#if NETFRAMEWORK
using System.Linq;
using System.Runtime.InteropServices;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Threading;
using MFDLabs.Networking;
using MFDLabs.Drawing;


using HWND = System.IntPtr;
#endif

namespace MFDLabs.Grid.Bot.Tasks.WorkQueues
{
    public sealed class GridServerScreenshotWorkQueue : AsyncWorkQueue<SocketTaskRequest>
    {
        private sealed class GridServerScreenshotWorkQueuePerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.WorkQueues.GridServerScreenshotWorkQueue";

            public IRawValueCounter TotalItemsProcessed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatFailed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
            public IAverageValueCounter GridServerScreenshotWorkQueueSuccessAverageTimeTicks { get; }
            public IAverageValueCounter GridServerScreenshotWorkQueueFailureAverageTimeTicks { get; }

            public GridServerScreenshotWorkQueuePerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
                TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedPerSecond", instance);
                TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
                TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatFailedPerSecond", instance);
                GridServerScreenshotWorkQueueSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "GridServerScreenshotWorkQueueSuccessAverageTimeTicks", instance);
                GridServerScreenshotWorkQueueFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "GridServerScreenshotWorkQueueFailureAverageTimeTicks", instance);
            }
        }

        private const string OnCareToLeakException = "An error occured with the grid server screenshot work queue task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:";

        private GridServerScreenshotWorkQueue()
            : base(WorkQueueDispatcherQueueRegistry.GridServerScreenshotQueue, OnReceive)
        { }

        // Doesn't break HATE SINGLETON because we never need multiple instances of this
        public static readonly GridServerScreenshotWorkQueue Singleton = new();

        private static readonly ConcurrentDictionary<ulong, UserWorkQueuePerformanceMonitor> _userPerformanceMonitors = new();
        private static UserWorkQueuePerformanceMonitor GetUserPerformanceMonitor(IUser user)
            => _userPerformanceMonitors.GetOrAdd(user.Id, _ => new UserWorkQueuePerformanceMonitor(PerfmonCounterRegistryProvider.Registry, "GridServerScreenshotWorkQueue", user));

        private static void HandleWorkQueueException(Exception ex, SocketMessage message, UserWorkQueuePerformanceMonitor perf)
        {
            global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);

            message.Author.FireEvent("GridServerScreenshotWorkQueueFailure", ex.ToDetailedString());

            perf.TotalItemsProcessedThatFailed.Increment();
            perf.TotalItemsProcessedThatFailedPerSecond.Increment();

#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Error(ex);
#else
            SystemLogger.Singleton.Warning("An error occurred when trying to execute grid server screenshot work queue task: {0}", ex.Message);
#endif

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    message.ReplyWithFile(
                        new MemoryStream(Encoding.UTF8.GetBytes(detail)),
                        "grid-server-screenshot-work-queue-ex.txt",
                        OnCareToLeakException
                    );
                    return;
                }

                message.Reply(
                    OnCareToLeakException,
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }
            message.Reply("An error occurred when trying to execute grid server screenshot work queue, please try again later.");
        }

        private static void OnReceive(SocketTaskRequest item, SuccessFailurePort result)
        {
            var message = item.Message;
            var perfmon = GetUserPerformanceMonitor(message.Author);

            try
            {
                perfmon.TotalItemsProcessed.Increment();
                perfmon.TotalItemsProcessedPerSecond.Increment();
                ProcessItem(item);
                perfmon.TotalItemsProcessedThatSucceeded.Increment();
                perfmon.TotalItemsProcessedThatSucceededPerSecond.Increment();
                result.Post(SuccessResult.Instance);
            }
            catch (Exception ex) { result.Post(ex); HandleWorkQueueException(ex, message, perfmon); }
        }

#region Metrics

        private static readonly GridServerScreenshotWorkQueuePerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

#endregion Metrics

#if NETFRAMEWORK

        private static void MaximizeGridServer([In] HWND hWnd)
        {
            const int SW_MAXIMIZE = 3;

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool ShowWindow(HWND hWnd, int nCmdShow);

            ShowWindow(hWnd, SW_MAXIMIZE);
        }

        private static void ScreenshotSingleGridServerAndRespond(SocketMessage message)
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

                message.ReplyWithFile(stream, fileName, "Grid Server Output:");
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        private static void ProcessSingleInstancedGridServerScreenshot(SocketMessage message)
        {
            using (message.Channel.EnterTypingState())
            {
                var tte = GridProcessHelper.OpenGridServerSafe().elapsed;

                if (tte.TotalSeconds > 1.5)
                {
                    // Wait for 1.25s so the grid server output can be populated.
                    TaskHelper.SetTimeoutFromMilliseconds(() => ScreenshotSingleGridServerAndRespond(message), 1250);
                    return;
                }

                ScreenshotSingleGridServerAndRespond(message);
            }
        }

#endif

        private static void ProcessItem(SocketTaskRequest item)
        {
            if (item == null) throw new ApplicationException("The task request was null.");

            var message = item.Message;
            var contentArray = item.ContentArray;

            _perfmon.TotalItemsProcessed.Increment();
            _perfmon.TotalItemsProcessedPerSecond.Increment();

            var sw = Stopwatch.StartNew();
            bool failure = false;

            try
            {
#if !NETFRAMEWORK
                SystemLogger.Singleton.Warning("Screenshots are not supported on this framework.");
                message.Reply("Grid Server Screenshots are not enabled at this time, please try again later.");
#else
                if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.SingleInstancedGridServer)
                {
                    ProcessSingleInstancedGridServerScreenshot(message);
                    return;
                }

                if (!contentArray.Any())
                {
                    var embed = message.ConstructUserLookupEmbed();
                    if (embed == null)
                    {
                        message.Reply("You haven't executed any scripts in this channel!");
                        return;
                    }

                    message.Reply(
                        $"Type `{(global::MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix)}viewconsole {{instanceId}}` to screenshot the specified instance.", 
                        embed: embed
                    );
                    return;
                }

                var clientIdx = contentArray.First();

                if (!int.TryParse(clientIdx, out var i))
                {
                    failure = true;
                    message.Reply($"The index '{contentArray.First()}' was not a valid integer.");
                    return;
                }

                var (stream, fileName, status, instance) = message.ScreenshotGridServer(i);

                switch (status)
                {
                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.NoRecentExecutions:
                        message.Reply("You haven't executed any scripts in this channel!");
                        break;
                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.UnkownId:
                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.NullInstance:
                        message.Reply($"There was no script execution found with the ID '{i}', " +
                                      $"re run the command with no arguments to see what executions you can screenshot.");
                        break;

                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.Success:
                        var expiration = instance.Expiration;
                        var timeStamp = new DateTimeOffset(expiration).ToUnixTimeSeconds();
                        message.ReplyWithFile(stream, fileName, $"This instance will expire at <t:{timeStamp}:T>");
                        break;

                    case GridServerArbiterScreenshotUtility.ScreenshotStatus.DisposedInstance:
                    default:
                        message.Reply("Internal Exception."); // for now, will figure out actual message later.
                        break;
                }
#endif
            }
            finally
            {
                sw.Stop();
                SystemLogger.Singleton.Debug("Took {0}s to execute grid server screenshot work queue task.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (failure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.GridServerScreenshotWorkQueueFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
                else
                {
                    _perfmon.GridServerScreenshotWorkQueueSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }
    }
}
