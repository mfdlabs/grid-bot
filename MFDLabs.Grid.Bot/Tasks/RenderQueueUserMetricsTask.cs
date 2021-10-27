using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Discord;
using MFDLabs.Analytics.Google;
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

namespace MFDLabs.Grid.Bot
{
    namespace Tasks
    {
        internal sealed class RenderQueueUserMetricsTask : ExpiringTaskThread<RenderQueueUserMetricsTask, RenderTaskRequest>
        {
            public override string Name => "Render Queue";
            public override TimeSpan ProcessActivationInterval => Settings.Singleton.RenderQueueDelay;
            public override TimeSpan Expiration => Settings.Singleton.RenderQueueExpiration;
            public override int PacketID => 5;

            public override PluginResult OnReceive(ref Packet<RenderTaskRequest> packet)
            {
                var perfmon = GetUserPerformanceMonitor(packet.Item.Message.Author);

                try
                {
                    perfmon.TotalRenders.Increment();
                    var result = SharedRenderQueueTaskPlugin.Singleton.OnReceive(ref packet);
                    perfmon.TotalRendersThatSucceeded.Increment();
                    return result;
                }
                catch (Exception ex)
                {
                    packet.Item.Message.Author.FireEvent("RenderQueueFailure", ex.ToDetailedString());
                    perfmon.TotalRendersThatFailed.Increment();
                    packet.Status = PacketProcessingStatus.Failure;
#if DEBUG
                    SystemLogger.Singleton.Error(ex);
#else
                    SystemLogger.Singleton.Warning("An error occurred when trying to execute render task: {0}", ex.Message);
#endif
                    if (!Settings.Singleton.CareToLeakSensitiveExceptions)
                    {
                        packet.Item.Message.Channel.SendMessage(
                            $"<@!{packet.Item.Message.Author.Id}>, An error occured with the reder task and the environment variable 'CareToLeakSensitiveExceptions' is false, this may leak sensitive information:",
                            options: new RequestOptions()
                            {
                                AuditLogReason = "Exception Occurred"
                            }
                        );
                        packet.Item.Message.Channel.SendMessage(embed: new EmbedBuilder().WithDescription($"```\n{ex.ToDetail()}\n```").Build());
                        return PluginResult.ContinueProcessing;
                    }
                    packet.Item.Message.Reply("An error occurred when trying to execute render task, please try again later.");
                    return PluginResult.ContinueProcessing;
                }
            }

            private RenderTaskUserPerformanceMonitor GetUserPerformanceMonitor(IUser user)
            {
                var perfmon = (from userPerfmon in _userPerformanceMonitors.OfType<(ulong, RenderTaskUserPerformanceMonitor)>() where userPerfmon.Item1 == user.Id select userPerfmon.Item2).FirstOrDefault();

                if (perfmon == default)
                {
                    perfmon = new RenderTaskUserPerformanceMonitor(CounterRegistry, user);
                    _userPerformanceMonitors.Add((user.Id, perfmon));
                }

                return perfmon;
            }

            private readonly ICollection<(ulong, RenderTaskUserPerformanceMonitor)> _userPerformanceMonitors = new List<(ulong, RenderTaskUserPerformanceMonitor)>();
        }
    }

    namespace Plugins
    {

        internal sealed class SharedRenderQueueTaskPlugin : BasePlugin<SharedRenderQueueTaskPlugin, RenderTaskRequest>
        {
            public override PluginResult OnReceive(ref Packet<RenderTaskRequest> packet)
            {
                return RenderExecutionTaskPlugin.Singleton.OnReceive(ref packet);
            }
        }

        // This cannot be an async task thread because it use locks in 2 different places. sorry
        //jakob: have you ever tried to write code that works? ha. never in a million years!!
        internal sealed class RenderExecutionTaskPlugin : BasePlugin<RenderExecutionTaskPlugin, RenderTaskRequest>
        {
            public RenderExecutionTaskPlugin()
                : base()
            {
                _perfmon = new RenderTaskPerformanceMonitor(StaticCounterRegistry.Instance); // simplify this init pls
            }

            #region Concurrency

            private readonly object _renderLock = new object();
            private bool _processingItem = false;
            private int _itemCount = 0;

            #endregion Concurrency

            #region Metrics

            private readonly RenderTaskPerformanceMonitor _perfmon;

            #endregion Metrics

            public override PluginResult OnReceive(ref Packet<RenderTaskRequest> packet)
            {
                var metrics = PacketMetricsPlugin<RenderTaskRequest>.Singleton.OnReceive(ref packet);

                if (metrics == PluginResult.StopProcessingAndDeallocate) return PluginResult.StopProcessingAndDeallocate;

                if (packet.Item != null)
                {

                    _perfmon.TotalItemsProcessed.Increment();
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        _itemCount++;
                        using (packet.Item.Message.Channel.EnterTypingState())
                        {
                            if (_processingItem)
                            {
                                packet.Item.Message.Reply($"The render queue is currently trying to process {_itemCount} items, please wait for your result to be processed.");
                            }

                            lock (_renderLock)
                            {
                                _processingItem = true;
                                if (packet.Item.OriginalCommandName == "sexually-weird-render")
                                {
                                    SystemLogger.Singleton.Warning("Someone found executed the one command, please no...");

                                    var (weirdStream, weirdFileName) = GridServerCommandUtility.Singleton.RenderUser(
                                        4,
                                        Settings.Singleton.RenderPlaceID,
                                        Settings.Singleton.RenderSizeX,
                                        Settings.Singleton.RenderSizeY
                                    );

                                    if (weirdStream == null || weirdFileName == null)
                                    {
                                        _perfmon.TotalItemsProcessedThatFailed.Increment();
                                        packet.Item.Message.Reply($"You are sending render requests too fast, please slow down!");
                                        return PluginResult.ContinueProcessing;
                                    }
                                    using (weirdStream)
                                        packet.Item.Message.Channel.SendFile(
                                            weirdStream,
                                            weirdFileName
                                        );
                                    return PluginResult.ContinueProcessing;
                                }

                                bool isAuthorCheck = false;
                                long userId = 0;

                                if (packet.Item.ContentArray.Length == 0)
                                {
                                    isAuthorCheck = true;
                                    var nullableUserID = packet.Item.Message.Author.GetRobloxID();

                                    if (!nullableUserID.HasValue)
                                    {
                                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                        SystemLogger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", packet.Item.Message.Author.ToString());
                                        packet.Item.Message.Reply($"You have no Roblox account associated with you.");
                                        return PluginResult.ContinueProcessing;
                                    }

                                    userId = nullableUserID.Value;
                                }


                                string username = null;

                                if (!isAuthorCheck)
                                    if (!long.TryParse(packet.Item.ContentArray.ElementAtOrDefault(0), out userId))
                                    {
                                        _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();

                                        if (packet.Item.Message.MentionedUsers.Count > 0)
                                        {
                                            var user = packet.Item.Message.MentionedUsers.ElementAt(0);
                                            // we have mentioned a user.
                                            var nullableUserID = user.GetRobloxID();

                                            if (!nullableUserID.HasValue)
                                            {
                                                _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                                SystemLogger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", user.ToString());
                                                packet.Item.Message.Reply($"The user you mentioned, '{user.Username}', had no Roblox account associated with them.");
                                                return PluginResult.ContinueProcessing;
                                            }

                                            userId = nullableUserID.Value;
                                        }
                                        else
                                        {
                                            SystemLogger.Singleton.Warning("The first parameter of the command was not a valid Int64, trying to get the userID by username lookup.");
                                            username = packet.Item.ContentArray.Join(' ').EscapeNewLines().Escape();
                                            if (!username.IsNullOrEmpty())
                                            {
                                                SystemLogger.Singleton.Debug("Trying to get the ID of the user by this username '{0}'", username);
                                                var nullableUserID = UserUtility.Singleton.GetUserIDByUsername(username);

                                                if (!nullableUserID.HasValue)
                                                {
                                                    _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                                    SystemLogger.Singleton.Warning("The ID for the user '{0}' was null, they were either banned or do not exist.", username);
                                                    packet.Item.Message.Reply($"The user by the username of '{username}' was not found.");
                                                    return PluginResult.ContinueProcessing;
                                                }

                                                SystemLogger.Singleton.Info("The ID for the user '{0}' was {1}.", username, nullableUserID.Value);
                                                userId = nullableUserID.Value;
                                            }
                                            else
                                            {
                                                _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                                                SystemLogger.Singleton.Warning("The user's input username was null or empty, they clearly do not know how to input text.");
                                                packet.Item.Message.Reply($"Missing required parameter 'userID' or 'userName', the layout is: {Settings.Singleton.Prefix}{packet.Item.OriginalCommandName} userID|userName");
                                                return PluginResult.ContinueProcessing;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (userId > Settings.Singleton.MaxUserIDSize)
                                        {
                                            _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                                            SystemLogger.Singleton.Warning("The input user ID of {0} was greater than the environment's maximum user ID size of {1}.", userId, Settings.Singleton.MaxUserIDSize);
                                            packet.Item.Message.Reply($"The userId '{userId}' is too big, expected the userId to be less than or equal to '{Settings.Singleton.MaxUserIDSize}'");
                                            return PluginResult.ContinueProcessing;
                                        }
                                    }

                                if (userId == -123123 && packet.Item.Message.Author.IsAdmin()) throw new Exception("Test exception for auto handling on task threads.");

                                if (UserUtility.Singleton.GetIsUserBanned(userId))
                                {
                                    bool canSkip = false;
                                    if (userId == -200000 && packet.Item.Message.Author.IsAdmin()) canSkip = true;

                                    if (!canSkip)
                                    {
                                        SystemLogger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                                        var user = userId == default ? username : userId.ToString();
                                        packet.Item.Message.Reply($"The user '{user}' is banned or does not exist.");
                                        return PluginResult.ContinueProcessing;
                                    }
                                }

                                SystemLogger.Singleton.Info(
                                    "Trying to render the character for the user '{0}' with the place '{1}', and the dimensions of {2}x{3}",
                                    userId,
                                    Settings.Singleton.RenderPlaceID,
                                    Settings.Singleton.RenderSizeX,
                                    Settings.Singleton.RenderSizeY
                                );

                                var (stream, fileName) = GridServerCommandUtility.Singleton.RenderUser(
                                    userId,
                                    Settings.Singleton.RenderPlaceID,
                                    Settings.Singleton.RenderSizeX,
                                    Settings.Singleton.RenderSizeY
                                );

                                if (stream == null || fileName == null)
                                {
                                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                                    packet.Item.Message.Reply($"You are sending render requests too fast, please slow down!");
                                    return PluginResult.ContinueProcessing;
                                }

                                using (stream)
                                    packet.Item.Message.Channel.SendFile(
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
                    SystemLogger.Singleton.Warning("Task packet {0} at the sequence {1} had a null item, ignoring...", packet.ID, packet.SequenceID);
                    if (Settings.Singleton.StopProcessingOnNullPacketItem) return PluginResult.StopProcessingAndDeallocate;
                    return PluginResult.ContinueProcessing;
                }
            }
        }
    }
}
