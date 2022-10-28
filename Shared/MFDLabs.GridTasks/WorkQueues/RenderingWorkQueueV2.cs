#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Microsoft.Ccr.Core;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.Threading;
using MFDLabs.Diagnostics;
using MFDLabs.Concurrency;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Reflection.Extensions;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;

namespace MFDLabs.Grid.Bot.WorkQueues
{
    public sealed class RenderingWorkQueueV2 : AsyncWorkQueue<SocketSlashCommand>
    {
        private sealed class RenderWorkQueueV2PerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.WorkQueues.RenderWorkQueueV2";

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
            public IAverageValueCounter RenderWorkQueueV2SuccessAverageTimeTicks { get; }
            public IAverageValueCounter RenderWorkQueueV2FailureAverageTimeTicks { get; }

            public RenderWorkQueueV2PerformanceMonitor(ICounterRegistry counterRegistry)
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
                RenderWorkQueueV2SuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderWorkQueueV2SuccessAverageTimeTicks", instance);
                RenderWorkQueueV2FailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderWorkQueueV2FailureAverageTimeTicks", instance);
            }
        }

        private const string OnCareToLeakException = "An error occured with the render work queue task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:";

        private RenderingWorkQueueV2()
            : base(WorkQueueDispatcherQueueRegistry.RenderQueue, OnReceive)
        { }

        // Doesn't break HATE SINGLETON because we never need multiple instances of this
        public static readonly RenderingWorkQueueV2 Singleton = new();

        private static readonly ConcurrentDictionary<ulong, UserWorkQueuePerformanceMonitor> _userPerformanceMonitors = new();
        private static UserWorkQueuePerformanceMonitor GetUserPerformanceMonitor(IUser user)
            => _userPerformanceMonitors.GetOrAdd(user.Id, _ => new UserWorkQueuePerformanceMonitor(PerfmonCounterRegistryProvider.Registry, "RenderingWorkQueueV2", user));

        private static void HandleWorkQueueException(Exception ex, SocketSlashCommand message, UserWorkQueuePerformanceMonitor perf)
        {
            global::MFDLabs.Grid.Bot.Utility.CrashHandler.Upload(ex, true);

            message.User.FireEvent("RenderWorkQueueFailure", ex.ToDetailedString());

            perf.TotalItemsProcessedThatFailed.Increment();
            perf.TotalItemsProcessedThatFailedPerSecond.Increment();

#if DEBUG || DEBUG_LOGGING_IN_PROD
            Logger.Singleton.Error(ex);
#else
            Logger.Singleton.Warning("An error occurred when trying to execute render work queue task: {0}", ex.Message);
#endif

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
            {
                var detail = ex.ToDetailedString();
                if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                {
                    message.RespondWithFilePublicPing(
                        new MemoryStream(Encoding.UTF8.GetBytes(detail)),
                        "render-work-queue-ex.txt",
                        OnCareToLeakException
                    );
                    return;
                }

                message.RespondPublicPing(
                    OnCareToLeakException,
                    embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                );
                return;
            }
            message.RespondPublicPing("An error occurred when trying to execute render task, please try again later.");
        }

        private static void OnReceive(SocketSlashCommand item, SuccessFailurePort result)
        {
            var perfmon = GetUserPerformanceMonitor(item.User);

            try
            {
                perfmon.TotalItemsProcessed.Increment();
                perfmon.TotalItemsProcessedPerSecond.Increment();
                ProcessItem(item);
                perfmon.TotalItemsProcessedThatSucceeded.Increment();
                perfmon.TotalItemsProcessedThatSucceededPerSecond.Increment();
                result.Post(SuccessResult.Instance);
            }
            catch (Exception ex) { result.Post(ex); HandleWorkQueueException(ex, item, perfmon); }
        }

        private static IEnumerable<string> BlacklistedUsernames =>
                (from uname in global::MFDLabs.Grid.Bot.Properties.Settings.Default.BlacklistedUsernamesForRendering.Split(',')
                 where !uname.IsNullOrEmpty()
                 select uname).ToArray();

        #region Concurrency

        private static readonly object _renderLock = new();
        private static bool _processingItem;
        private static Atomic<int> _itemCount = 0;

        #endregion Concurrency

        #region Metrics

        private static readonly RenderWorkQueueV2PerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

        private static long GetUserId(
            ref SocketSlashCommand item,
            ref SocketSlashCommandDataOption subCommand, 
            ref string option,
            ref bool failure
        )
        {
            var userId = 0L;

            switch (option.ToLower())
            {
                case "roblox_id":
                    var nullableUserId = subCommand.GetOptionValue("id")?.ToInt64();
                    if (!nullableUserId.HasValue)
                    {
                        _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                        _perfmon.TotalItemsProcessedThatHadInvalidUserIDsPerSecond.Increment();

                        Logger.Singleton.Warning("Null ID given for /render roblox_id, this is not expected.");
                        item.RespondEphemeralPing("Internal exception while rendering.");
                        return long.MinValue;
                    }

                    userId = nullableUserId.Value;
                    break;
                case "roblox_name":
                    var username = subCommand.GetOptionValue("user_name")?.ToString()?.EscapeNewLines()?.Escape();

                    if (username.IsNullOrEmpty())
                    {
                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond.Increment();
                        failure = true;

                        Logger.Singleton.Warning("The user's input username was null or empty, they clearly do not know how to input text.");
                        item.RespondEphemeralPing($"Missing required parameter 'user_name'.");
                        return long.MinValue;
                    }

                    Logger.Singleton.Debug("Trying to get the ID of the user by this username '{0}'", username);

                    if (BlacklistedUsernames.Contains(username))
                    {
                        _perfmon.TotalItemsProcessedThatHadBlacklistedUsernames.Increment();
                        _perfmon.TotalItemsProcessedThatHadBlacklistedUsernamesPerSecond.Increment();
                        failure = true;

                        item.RespondEphemeralPing($"The username '{username}' is a blacklisted username, please try again later.");
                        return long.MinValue;
                    }

                    var nullableUserIdRemote = UserUtility.GetUserIdByUsername(username);
                    if (!nullableUserIdRemote.HasValue)
                    {
                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                        failure = true;

                        Logger.Singleton.Warning("The ID for the user '{0}' was null, they were either banned or do not exist.", username);
                        item.RespondEphemeralPing($"The user by the username of '{username}' was not found.");
                        return long.MinValue;
                    }

                    Logger.Singleton.Info("The ID for the user '{0}' was {1}.", username, nullableUserIdRemote.Value);
                    userId = nullableUserIdRemote.Value;

                    break;

                case "discord_user":

                    var userRef = (IUser)subCommand.GetOptionValue("user");
                    if (userRef == null)
                    {
                        _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                        _perfmon.TotalItemsProcessedThatHadInvalidUserIDsPerSecond.Increment();

                        Logger.Singleton.Warning("Null User Ref given for /render discord_user, this is not expected.");
                        item.RespondEphemeralPing("Internal exception while rendering.");
                        return long.MinValue;
                    }

                    var nullableUserIdFromUser = userRef.GetRobloxId();

                    if (!nullableUserIdFromUser.HasValue)
                    {
                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                        failure = true;

                        Logger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", userRef.ToString());
                        item.RespondEphemeralPing($"The user you mentioned, '{userRef.Username}', had no Roblox account associated with them.");
                        return long.MinValue;
                    }

                    userId = nullableUserIdFromUser.Value;

                    break;

                case "self":

                    var nullableUserIdFromAuthor = item.User.GetRobloxId();

                    if (!nullableUserIdFromAuthor.HasValue)
                    {
                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                        failure = true;

                        Logger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", item.User.ToString());
                        item.RespondEphemeralPing("You have no Roblox account associated with you.");
                        return long.MinValue;
                    }

                    userId = nullableUserIdFromAuthor.Value;

                    break;
            }

            return userId;
        }

        private static void ProcessItem(SocketSlashCommand item)
        {
            if (item == null) throw new ApplicationException("The task request was null.");

            _perfmon.TotalItemsProcessed.Increment();
            _perfmon.TotalItemsProcessedPerSecond.Increment();

            var sw = Stopwatch.StartNew();
            bool failure = false;

            try
            {
                _itemCount++;

                using (item.DeferPublic())
                {
                    if (_processingItem)
                        item.RespondEphemeralPing($"The render work queue is currently trying to process {_itemCount} items, please wait for your result to be processed.");

                    lock (_renderLock)
                    {
                        _processingItem = true;

                        var subCommand = item.Data.GetSubCommand();
                        var option = subCommand.Name;

                        var userId = GetUserId(ref item, ref subCommand, ref option, ref failure);
                        if (userId == long.MinValue) return;

                        if (UserUtility.GetIsUserBanned(userId))
                        {
                            Logger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                            item.RespondEphemeralPing($"The user '{userId}' is banned or does not exist.");
                            return;
                        }

                        Logger.Singleton.Info(
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
                            item.RespondEphemeralPing("Internal failure when processing item render.");

                            return;
                        }

                        using (stream)
                            item.RespondWithFilePublicPing(
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
                Logger.Singleton.Debug("Took {0}s to execute render work queue task.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (failure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.RenderWorkQueueV2FailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                } else
                {
                    _perfmon.RenderWorkQueueV2SuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }

    }
}

#endif