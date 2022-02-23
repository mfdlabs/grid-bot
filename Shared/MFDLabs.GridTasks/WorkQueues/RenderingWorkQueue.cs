using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Ccr.Core;
using Discord;
using Discord.WebSocket;
using MFDLabs.Concurrency;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Threading;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Instrumentation;
using MFDLabs.Diagnostics;

namespace MFDLabs.Grid.Bot.WorkQueues
{
    public sealed class RenderingWorkQueue : AsyncWorkQueue<SocketTaskRequest>
    {
        private sealed class RenderWorkQueuePerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.WorkQueues.RenderWorkQueue";

            public IRawValueCounter TotalItemsProcessed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatFailed { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatFailedPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatHadInvalidUserIDs { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadInvalidUserIDsPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatHadNullOrEmptyUsernames { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond { get; }
            public IRawValueCounter TotalItemsProcessedThatHadBlacklistedUsernames { get; }
            public IRateOfCountsPerSecondCounter TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond { get; }
            public IAverageValueCounter RenderWorkQueueSuccessAverageTimeTicks { get; }
            public IAverageValueCounter RenderWorkQueueFailureAverageTimeTicks { get; }

            public RenderWorkQueuePerformanceMonitor(ICounterRegistry counterRegistry)
            {
                if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));

                var instance = $"{SystemGlobal.GetMachineId()} ({SystemGlobal.GetMachineHost()})";

                TotalItemsProcessed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessed", instance);
                TotalItemsProcessedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedPerSecond", instance);
                TotalItemsProcessedThatFailed = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatFailed", instance);
                TotalItemsProcessedThatFailedPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatFailedPerSecond", instance);
                TotalItemsProcessedThatHadInvalidUserIDs = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadInvalidUserIDs", instance);
                TotalItemsProcessedThatHadInvalidUserIDsPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadInvalidUserIDsPerSecond", instance);
                TotalItemsProcessedThatHadNullOrEmptyUsernames = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadNullOrEmptyUsernames", instance);
                TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond", instance);
                TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount = counterRegistry.GetRawValueCounter(
                    Category,
                    "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount",
                    instance
                );
                TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(
                    Category,
                    "TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond",
                    instance
                );
                TotalItemsProcessedThatHadBlacklistedUsernames = counterRegistry.GetRawValueCounter(Category, "TotalItemsProcessedThatHadBlacklistedUsernames", instance);
                TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond = counterRegistry.GetRateOfCountsPerSecondCounter(Category, "TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond", instance);
                RenderWorkQueueSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderWorkQueueSuccessAverageTimeTicks", instance);
                RenderWorkQueueFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderWorkQueueFailureAverageTimeTicks", instance);
            }
        }

        private const string OnCareToLeakException = "An error occured with the render work queue task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:";

        private RenderingWorkQueue()
            : base(WorkQueueDispatcherQueueRegistry.RenderQueue, OnReceive)
        { }

        // Doesn't break HATE SINGLETON because we never need multiple instances of this
        public static readonly RenderingWorkQueue Singleton = new();

        private static readonly ConcurrentDictionary<ulong, UserWorkQueuePerformanceMonitor> _userPerformanceMonitors = new();
        private static UserWorkQueuePerformanceMonitor GetUserPerformanceMonitor(IUser user)
            => _userPerformanceMonitors.GetOrAdd(user.Id, _ => new UserWorkQueuePerformanceMonitor(PerfmonCounterRegistryProvider.Registry, "RenderingWorkQueue", user));

        private static void HandleWorkQueueException(Exception ex, SocketMessage message, UserWorkQueuePerformanceMonitor perf)
        {
            global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);

            message.Author.FireEvent("RenderWorkQueueFailure", ex.ToDetailedString());

            perf.TotalItemsProcessedThatFailed.Increment();
            perf.TotalItemsProcessedThatFailedPerSecond.Increment();

#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Error(ex);
#else
            SystemLogger.Singleton.Warning("An error occurred when trying to execute render work queue task: {0}", ex.Message);
#endif

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    message.ReplyWithFile(
                        new MemoryStream(Encoding.UTF8.GetBytes(detail)),
                        "render-work-queue-ex.txt",
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
            message.Reply("An error occurred when trying to execute render task, please try again later.");
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

        private static IEnumerable<string> BlacklistedUsernames =>
                (from uname in global::MFDLabs.Grid.Bot.Properties.Settings.Default.BlacklistedUsernamesForRendering.Split(',')
                 where !uname.IsNullOrEmpty()
                 select uname).ToArray();

        #region Concurrency

        private static readonly object _renderLock = new();
        private static bool _processingItem;
        private static Atomic _itemCount = 0;

        #endregion Concurrency

        #region Metrics

        private static readonly RenderWorkQueuePerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

        private static void ProcessItem(SocketTaskRequest item)
        {
            if (item == null) throw new ApplicationException("The task request was null.");

            var message = item.Message;
            var contentArray = item.ContentArray;
            var originalCommandName = item.OriginalCommandName;

            _perfmon.TotalItemsProcessed.Increment();
            _perfmon.TotalItemsProcessedPerSecond.Increment();

            var sw = Stopwatch.StartNew();
            bool failure = false;

            try
            {
                _itemCount++;

                using (message.Channel.EnterTypingState())
                {
                    if (_processingItem)
                        message.Reply($"The render work queue is currently trying to process {_itemCount} items, please wait for your result to be processed.");

                    lock (_renderLock)
                    {
                        _processingItem = true;

                        if (originalCommandName == "sexually-weird-render")
                        {
                            SystemLogger.Singleton.Warning("Got the test render command.");

                            var (weirdStream, weirdFileName) = GridServerCommandUtility.RenderUser(
                                4,
                                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderPlaceID,
                                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeX,
                                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeY
                            );

                            if (weirdStream == null || weirdFileName == null)
                            {
                                failure = true;

                                message.Reply("Internal failure when processing item render.");

                                return;
                            }

                            using (weirdStream)
                                message.ReplyWithFile(
                                    weirdStream,
                                    weirdFileName,
                                    "Render Test"
                                );

                            return;
                        }

                        var isAuthorCheck = false;
                        long userId = 0;

                        if (contentArray.Length == 0)
                        {
                            isAuthorCheck = true;

                            var nullableUserId = message.Author.GetRobloxId();

                            if (!nullableUserId.HasValue)
                            {
                                _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                                failure = true;

                                SystemLogger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", message.Author.ToString());
                                message.Reply("You have no Roblox account associated with you.");
                                return;
                            }

                            userId = nullableUserId.Value;
                        }

                        string username = null;

                        if (!isAuthorCheck)
                            if (!long.TryParse(contentArray.ElementAtOrDefault(0), out userId))
                            {
                                _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();

                                if (message.MentionedUsers.Count > 0)
                                {
                                    var user = message.MentionedUsers.ElementAt(0);
                                    // we have mentioned a user.
                                    var nullableUserId = user.GetRobloxId();

                                    if (!nullableUserId.HasValue)
                                    {
                                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                                        failure = true;

                                        SystemLogger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", user.ToString());
                                        message.Reply($"The user you mentioned, '{user.Username}', had no Roblox account associated with them.");
                                        return;
                                    }

                                    userId = nullableUserId.Value;
                                }
                                else
                                {
                                    SystemLogger.Singleton.Warning("The first parameter of the command was " +
                                                                   "not a valid Int64, trying to get the userID " +
                                                                   "by username lookup.");
                                    username = contentArray.Join(' ').EscapeNewLines().Escape();

                                    if (BlacklistedUsernames.Contains(username))
                                    {
                                        _perfmon.TotalItemsProcessedThatHadBlacklistedUsernames.Increment();
                                        _perfmon.TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond.Increment();
                                        failure = true;

                                        message.Reply($"The username '{username}' is a blacklisted username, please try again later.");
                                        return;
                                    }

                                    if (!username.IsNullOrEmpty())
                                    {
                                        SystemLogger.Singleton.Debug("Trying to get the ID of the user by this username '{0}'", username);
                                        var nullableUserId = UserUtility.GetUserIdByUsername(username);

                                        if (!nullableUserId.HasValue)
                                        {
                                            _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                            _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                                            failure = true;

                                            SystemLogger.Singleton.Warning("The ID for the user '{0}' was null, they were either banned or do not exist.", username);
                                            message.Reply($"The user by the username of '{username}' was not found.");
                                            return;
                                        }

                                        SystemLogger.Singleton.Info("The ID for the user '{0}' was {1}.", username, nullableUserId.Value);
                                        userId = nullableUserId.Value;
                                    }
                                    else
                                    {
                                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond.Increment();
                                        failure = true;

                                        SystemLogger.Singleton.Warning("The user's input username was null or empty, they clearly do not know how to input text.");
                                        message.Reply($"Missing required parameter 'userID' or 'userName', " +
                                                      $"the layout is: " +
                                                      $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}{originalCommandName} " +
                                                      $"userID|userName");
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                if (userId > global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize)
                                {
                                    _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                                    _perfmon.TotalItemsProcessedThatHadInvalidUserIDsPerSecond.Increment();
                                    failure = true;

                                    SystemLogger.Singleton.Warning(
                                        "The input user ID of {0} was greater than the environment's maximum user ID size of {1}.",
                                        userId,
                                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize
                                    );
                                    message.Reply($"The userId '{userId}' is too big, expected the " +
                                                  $"userId to be less than or equal to " +
                                                  $"'{MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize}'");
                                    return;
                                }
                            }

                        if (UserUtility.GetIsUserBanned(userId))
                        {
                            SystemLogger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                            var user = userId == default ? username : userId.ToString();
                            message.Reply($"The user '{user}' is banned or does not exist.");
                            return;
                        }

                        SystemLogger.Singleton.Info(
                                    "Trying to render the character for the user '{0}' with the place '{1}', " +
                                    "and the dimensions of {2}x{3}",
                                    userId,
                                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderPlaceID,
                                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeX,
                                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeY
                                );

                        // get a stream and temp filename
                        var (stream, fileName) = GridServerCommandUtility.RenderUser(
                            userId,
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderPlaceID,
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeX,
                            global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeY
                        );

                        if (stream == null || fileName == null)
                        {
                            failure = true;
                            message.Reply("Internal failure when processing item render.");

                            return;
                        }

                        using (stream)
                            message.ReplyWithFile(
                                stream,
                                fileName
                            );
                    }
                }
            }
            finally
            {
                _itemCount--;
                _processingItem = false;
                sw.Stop();
                SystemLogger.Singleton.Debug("Took {0}s to execute render work queue task.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (failure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.RenderWorkQueueFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                } else
                {
                    _perfmon.RenderWorkQueueSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }

    }
}
