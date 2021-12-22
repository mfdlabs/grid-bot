using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Discord;
using MFDLabs.Concurrency;
using MFDLabs.Concurrency.Base;
using MFDLabs.ErrorHandling.Extensions;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Models;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Grid.Bot.Plugins;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Instrumentation;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

#if WE_LOVE_EM_SLASH_COMMANDS
using Discord.WebSocket;
#endif

namespace MFDLabs.Grid.Bot
{
    namespace Tasks
    {

#if WE_LOVE_EM_SLASH_COMMANDS

        internal sealed class RenderQueueSlashCommandUserMetricsTask : ExpiringTaskThread<RenderQueueSlashCommandUserMetricsTask, SocketSlashCommand>
        {
            protected override string Name => "Render Queue V2";
            protected override TimeSpan ProcessActivationInterval => global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderQueueDelay;
            protected override TimeSpan Expiration => global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderQueueExpiration;
            protected override ICounterRegistry CounterRegistry => PerfmonCounterRegistryProvider.Registry;
            protected override int PacketId => 6;

            public override PluginResult OnReceive(ref Packet<SocketSlashCommand> packet)
            {
                var message = packet.Item;
                var perfmon = GetUserPerformanceMonitor(message.User);

                try
                {
                    perfmon.TotalItemsProcessed.Increment();
                    var result = RenderExecutionSlashCommandTaskPlugin.Singleton.OnReceive(ref packet);
                    perfmon.TotalItemsProcessedThatSucceeded.Increment();
                    return result;
                }
                catch (Exception ex)
                {
                    message.User.FireEvent("RenderQueueV2Failure", ex.ToDetailedString());
                    perfmon.TotalItemsProcessedThatFailed.Increment();
                    packet.Status = PacketProcessingStatus.Failure;
#if DEBUG
                    SystemLogger.Singleton.Error(ex);
#else
                    SystemLogger.Singleton.Warning("An error occurred when trying to execute render task: {0}", ex.Message);
#endif
                    if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
                    {
                        var detail = ex.ToDetailedString();
                        if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                        {
                            message.RespondWithFileEphemeral(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt");
                            return PluginResult.ContinueProcessing;
                        }

                        message.RespondEphemeral(
                            "An error occured with the render execution task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                            embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                        );
                        return PluginResult.ContinueProcessing;
                    }
                    message.RespondEphemeral("An error occurred when trying to execute render task, please try again later.");
                    return PluginResult.ContinueProcessing;
                }
            }

            private UserTaskPerformanceMonitor GetUserPerformanceMonitor(IUser user)
            {
                var perfmon = (from userPerfmon in _userPerformanceMonitors.OfType<(ulong, UserTaskPerformanceMonitor)>() where userPerfmon.Item1 == user.Id select userPerfmon.Item2).FirstOrDefault();

                if (perfmon == default)
                {
                    perfmon = new UserTaskPerformanceMonitor(CounterRegistry, "RenderTask", user);
                    _userPerformanceMonitors.Add((user.Id, perfmon));
                }

                return perfmon;
            }

            private readonly ICollection<(ulong, UserTaskPerformanceMonitor)> _userPerformanceMonitors = new List<(ulong, UserTaskPerformanceMonitor)>();
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        internal sealed class RenderQueueUserMetricsTask : ExpiringTaskThread<RenderQueueUserMetricsTask, SocketTaskRequest>
        {
            protected override string Name => "Render Queue";
            protected override TimeSpan ProcessActivationInterval => global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderQueueDelay;
            protected override TimeSpan Expiration => global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderQueueExpiration;
            protected override ICounterRegistry CounterRegistry => PerfmonCounterRegistryProvider.Registry;
            protected override int PacketId => 5;

            public override PluginResult OnReceive(ref Packet<SocketTaskRequest> packet)
            {
                var message = packet.Item.Message;
                var perfmon = GetUserPerformanceMonitor(message.Author);

                try
                {
                    perfmon.TotalItemsProcessed.Increment();
                    var result = RenderExecutionTaskPlugin.Singleton.OnReceive(ref packet);
                    perfmon.TotalItemsProcessedThatSucceeded.Increment();
                    return result;
                }
                catch (Exception ex)
                {
                    message.Author.FireEvent("RenderQueueFailure", ex.ToDetailedString());
                    perfmon.TotalItemsProcessedThatFailed.Increment();
                    packet.Status = PacketProcessingStatus.Failure;
#if DEBUG
                    SystemLogger.Singleton.Error(ex);
#else
                    SystemLogger.Singleton.Warning("An error occurred when trying to execute render task: {0}", ex.Message);
#endif
                    if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.CareToLeakSensitiveExceptions)
                    {
                        var detail = ex.ToDetailedString();
                        if (detail.Length > EmbedBuilder.MaxDescriptionLength)
                        {
                            message.ReplyWithFile(new MemoryStream(Encoding.UTF8.GetBytes(detail)), "ex.txt");
                            return PluginResult.ContinueProcessing;
                        }

                        message.Reply(
                            "An error occured with the render execution task and the environment variable " +
                            "'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                            embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build()
                        );
                        return PluginResult.ContinueProcessing;
                    }
                    message.Reply("An error occurred when trying to execute render task, please try again later.");
                    return PluginResult.ContinueProcessing;
                }
            }

            private UserTaskPerformanceMonitor GetUserPerformanceMonitor(IUser user)
            {
                var perfmon = (from userPerfmon in _userPerformanceMonitors
                    where userPerfmon.Item1 == user.Id
                    select userPerfmon.Item2).FirstOrDefault();

                if (perfmon != default) return perfmon;
                
                perfmon = new UserTaskPerformanceMonitor(CounterRegistry, "RenderTask", user);
                _userPerformanceMonitors.Add((user.Id, perfmon));

                return perfmon;
            }

            private readonly ICollection<(ulong, UserTaskPerformanceMonitor)> _userPerformanceMonitors = new List<(ulong, UserTaskPerformanceMonitor)>();
        }
    }

    namespace Plugins
    {
#if WE_LOVE_EM_SLASH_COMMANDS

        // This cannot be an async task thread because it use locks in 2 different places. sorry
        //jakob: have you ever tried to write code that works? ha. never in a million years!!
        internal sealed class RenderExecutionSlashCommandTaskPlugin : BasePlugin<RenderExecutionSlashCommandTaskPlugin, SocketSlashCommand>
        {
        #region Concurrency

            private readonly object _renderLock = new object();
            private bool _processingItem = false;
            private int _itemCount = 0;

        #endregion Concurrency

        #region Metrics

            private readonly RenderTaskPerformanceMonitor _perfmon = new RenderTaskPerformanceMonitor(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

            public override PluginResult OnReceive(ref Packet<SocketSlashCommand> packet)
            {
                var metrics = PacketMetricsPlugin<SocketSlashCommand>.Singleton.OnReceive(ref packet);

                if (metrics == PluginResult.StopProcessingAndDeallocate) return PluginResult.StopProcessingAndDeallocate;

                if (packet.Item != null)
                {
                    var message = packet.Item;

                    _perfmon.TotalItemsProcessed.Increment();
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        _itemCount++;
                        using (message.Channel.EnterTypingState())
                        {
                            if (_processingItem)
                            {
                                message.RespondEphemeral($"The render queue is currently trying to process {_itemCount} items, please wait for your result to be processed.");
                            }

                            lock (_renderLock)
                            {
                                _processingItem = true;

                                long userId = Convert.ToInt64((from uid in message.Data.Options where uid.Name == "user_id" select uid).FirstOrDefault().Value);


                                if (userId > global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize)
                                {
                                    _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                                    SystemLogger.Singleton.Warning("The input user ID of {0} was greater than the environment's maximum user ID size of {1}.", userId, global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize);
                                    message.RespondEphemeral($"The userId '{userId}' is too big, expected the userId to be less than or equal to '{MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize}'");
                                    return PluginResult.ContinueProcessing;
                                }


                                if (userId == -123123 && message.User.IsAdmin()) throw new Exception("Test exception for auto handling on task threads.");

                                if (UserUtility.GetIsUserBanned(userId))
                                {
                                    var canSkip = userId == -200000 && message.User.IsAdmin();

                                    if (!canSkip)
                                    {
                                        SystemLogger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                                        message.RespondEphemeral($"The user '{userId}' is banned or does not exist.");
                                        return PluginResult.ContinueProcessing;
                                    }
                                }

                                SystemLogger.Singleton.Info(
                                    "Trying to render the character for the user '{0}' with the place '{1}', and the dimensions of {2}x{3}",
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
                                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                                    message.RespondEphemeral($"You are sending render requests too fast, please slow down!");
                                    return PluginResult.ContinueProcessing;
                                }

                                using (stream)
                                    message.RespondWithFileEphemeral(
                                        stream,
                                        fileName
                                    );

                                return PluginResult.ContinueProcessing;
                            }
                        }
                    }
                    finally
                    {
                        _itemCount--;
                        _processingItem = false;
                        sw.Stop();
                        SystemLogger.Singleton.Debug("Took {0}s to execute render task.", sw.Elapsed.TotalSeconds.ToString("f7"));
                    }
                }
                else
                {
                    SystemLogger.Singleton.Warning("Task packet {0} at the sequence {1} had a null item, ignoring...", packet.Id, packet.SequenceId);
                    if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.StopProcessingOnNullPacketItem) return PluginResult.StopProcessingAndDeallocate;
                    return PluginResult.ContinueProcessing;
                }
            }
        }

#endif // WE_LOVE_EM_SLASH_COMMANDS

        // This cannot be an async task thread because it use locks in 2 different places. sorry
        //jakob: have you ever tried to write code that works? ha. never in a million years!!
        internal sealed class RenderExecutionTaskPlugin : BasePlugin<RenderExecutionTaskPlugin, SocketTaskRequest>
        {
            #region Concurrency

            private readonly object _renderLock = new();
            private bool _processingItem;
            private int _itemCount;

            #endregion Concurrency

            #region Metrics

            private readonly RenderTaskPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

            #endregion Metrics

            public override PluginResult OnReceive(ref Packet<SocketTaskRequest> packet)
            {
                var metrics = PacketMetricsPlugin<SocketTaskRequest>.Singleton.OnReceive(ref packet);

                if (metrics == PluginResult.StopProcessingAndDeallocate) return PluginResult.StopProcessingAndDeallocate;

                if (packet.Item != null)
                {
                    var message = packet.Item.Message;
                    var contentArray = packet.Item.ContentArray;
                    var originalCommandName = packet.Item.OriginalCommandName;

                    _perfmon.TotalItemsProcessed.Increment();
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        _itemCount++;
                        using (message.Channel.EnterTypingState())
                        {
                            if (_processingItem)
                            {
                                message.Reply($"The render queue is currently trying to process {_itemCount} items," +
                                              $" please wait for your result to be processed.");
                            }

                            lock (_renderLock)
                            {
                                _processingItem = true;
                                if (originalCommandName == "sexually-weird-render")
                                {
                                    SystemLogger.Singleton.Warning("Someone found executed the one command, please no...");

                                    var (weirdStream, weirdFileName) = GridServerCommandUtility.RenderUser(
                                        4,
                                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderPlaceID,
                                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeX,
                                        global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderSizeY
                                    );

                                    if (weirdStream == null || weirdFileName == null)
                                    {
                                        _perfmon.TotalItemsProcessedThatFailed.Increment();
                                        message.Reply($"You are sending render requests too fast, please slow down!");
                                        return PluginResult.ContinueProcessing;
                                    }
                                    using (weirdStream)
                                        message.ReplyWithFile(
                                            weirdStream,
                                            weirdFileName
                                        );
                                    return PluginResult.ContinueProcessing;
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
                                        SystemLogger.Singleton.Warning("The ID for the discord user '{0}' was null," +
                                                                       " they were either banned or do not exist.",
                                            message.Author.ToString());
                                        message.Reply("You have no Roblox account associated with you.");
                                        return PluginResult.ContinueProcessing;
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
                                                SystemLogger.Singleton.Warning("The ID for the discord user '{0}' " +
                                                                               "was null, they were either banned or do not exist.",
                                                    user.ToString());
                                                message.Reply($"The user you mentioned, '{user.Username}', had no " +
                                                              $"Roblox account associated with them.");
                                                return PluginResult.ContinueProcessing;
                                            }

                                            userId = nullableUserId.Value;
                                        }
                                        else
                                        {
                                            SystemLogger.Singleton.Warning("The first parameter of the command was " +
                                                                           "not a valid Int64, trying to get the userID " +
                                                                           "by username lookup.");
                                            username = contentArray.Join(' ').EscapeNewLines().Escape();
                                            if (!username.IsNullOrEmpty())
                                            {
                                                SystemLogger.Singleton.Debug("Trying to get the ID of the user by " +
                                                                             "this username '{0}'", username);
                                                var nullableUserId = UserUtility.GetUserIdByUsername(username);

                                                if (!nullableUserId.HasValue)
                                                {
                                                    _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                                    SystemLogger.Singleton.Warning("The ID for the user '{0}' was " +
                                                        "null, they were either banned or do not exist.", username);
                                                    message.Reply($"The user by the username of '{username}' was not found.");
                                                    return PluginResult.ContinueProcessing;
                                                }

                                                SystemLogger.Singleton.Info("The ID for the user '{0}' was {1}.", username, nullableUserId.Value);
                                                userId = nullableUserId.Value;
                                            }
                                            else
                                            {
                                                _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                                                SystemLogger.Singleton.Warning("The user's input username was null " +
                                                                               "or empty, they clearly do not know how to input text.");
                                                message.Reply($"Missing required parameter 'userID' or 'userName', " +
                                                              $"the layout is: " +
                                                              $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}{originalCommandName} " +
                                                              $"userID|userName");
                                                return PluginResult.ContinueProcessing;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (userId > global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize)
                                        {
                                            _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                                            SystemLogger.Singleton.Warning("The input user ID of {0} was greater " +
                                                                           "than the environment's maximum user ID size of {1}.",
                                                userId,
                                                global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize);
                                            message.Reply($"The userId '{userId}' is too big, expected the " +
                                                          $"userId to be less than or equal to " +
                                                          $"'{MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize}'");
                                            return PluginResult.ContinueProcessing;
                                        }
                                    }

                                if (userId == -123123 && message.Author.IsAdmin()) throw new Exception("Test " +
                                    "exception for auto handling on task threads.");

                                if (UserUtility.GetIsUserBanned(userId))
                                {
                                    bool canSkip = userId == -200000 && message.Author.IsAdmin();

                                    if (!canSkip)
                                    {
                                        SystemLogger.Singleton.Warning("The input user ID of {0} was linked to a " +
                                                                       "banned user account.", userId);
                                        var user = userId == default ? username : userId.ToString();
                                        message.Reply($"The user '{user}' is banned or does not exist.");
                                        return PluginResult.ContinueProcessing;
                                    }
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
                                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                                    message.Reply($"You are sending render requests too fast, please slow down!");
                                    return PluginResult.ContinueProcessing;
                                }

                                using (stream)
                                    message.ReplyWithFile(
                                        stream,
                                        fileName
                                    );

                                return PluginResult.ContinueProcessing;
                            }
                        }
                    }
                    finally
                    {
                        _itemCount--;
                        _processingItem = false;
                        sw.Stop();
                        SystemLogger.Singleton.Debug("Took {0}s to execute render task.",
                            sw.Elapsed.TotalSeconds.ToString("f7"));
                    }
                }
                else
                {
                    SystemLogger.Singleton.Warning("Task packet {0} at the sequence {1} had a null item, ignoring...", packet.Id, packet.SequenceId);
                    return global::MFDLabs.Grid.Bot.Properties.Settings.Default.StopProcessingOnNullPacketItem
                        ? PluginResult.StopProcessingAndDeallocate
                        : PluginResult.ContinueProcessing;
                }
            }
        }


    }
}
