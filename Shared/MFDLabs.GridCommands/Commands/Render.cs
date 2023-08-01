using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Threading;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Instrumentation;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal class Render : IStateSpecificCommandHandler
    {
        public string CommandName => "Render User";
        public string CommandDescription => $"If no arguments are given, it will try to get the Roblox ID " +
                                            $"for the author and render them.\nLayout: " +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}render " +
                                            $"robloxUserID?|discordUserMention?|...userName?";
        public string[] CommandAliases => new[] { "r", "render" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        private sealed class RenderCommandPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.Commands.Render";

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
            public IAverageValueCounter RenderCommandSuccessAverageTimeTicks { get; }
            public IAverageValueCounter RenderCommandFailureAverageTimeTicks { get; }

            public RenderCommandPerformanceMonitor(ICounterRegistry counterRegistry)
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
                RenderCommandSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderCommandSuccessAverageTimeTicks", instance);
                RenderCommandFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderCommandFailureAverageTimeTicks", instance);
            }
        }

        // language=regex
        private const string GoodUsernameRegex = @"^[A-Za-z0-9_]{3,20}$";

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

        private static readonly RenderCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

        public async Task Invoke(string[] contentArray, SocketMessage message, string originalCommandName)
        {
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
                        await message.ReplyAsync($"The render queue is currently trying to process {_itemCount} items, please wait for your result to be processed.");

                    lock (_renderLock)
                    {
                        _processingItem = true;

                        var isAuthorCheck = false;
                        long userId = 0;

                        if (contentArray.Length == 0)
                        {
#if FEATURE_RBXDISCORDUSERS_CLIENT
                            isAuthorCheck = true;

                            var nullableUserId = message.Author.GetRobloxId();

                            if (!nullableUserId.HasValue)
                            {
                                _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                                failure = true;

                                Logger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", message.Author.ToString());
                                message.Reply("You have no Roblox account associated with you.");
                                return;
                            }

                            userId = nullableUserId.Value;
#else
                            message.Reply("Calling the render command like this is deprecated until further notice. Please see https://github.com/mfdlabs/grid-bot-support/discussions/13.");
                            return;
#endif
                        }

                        string username = null;

                        if (!isAuthorCheck)
                            if (!long.TryParse(contentArray.ElementAtOrDefault(0), out userId))
                            {
                                _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();

                                if (message.MentionedUsers.Count > 0)
                                {
#if FEATURE_RBXDISCORDUSERS_CLIENT
                                    var user = message.MentionedUsers.ElementAt(0);
                                    // we have mentioned a user.
                                    var nullableUserId = user.GetRobloxId();

                                    if (!nullableUserId.HasValue)
                                    {
                                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                        _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                                        failure = true;

                                        Logger.Singleton.Warning("The ID for the discord user '{0}' was null, they were either banned or do not exist.", user.ToString());
                                        message.Reply($"The user you mentioned, '{user.Username}', had no Roblox account associated with them.");
                                        return;
                                    }

                                    userId = nullableUserId.Value;
#else
                                    message.Reply("Calling the render command like this is deprecated until further notice. Please see https://github.com/mfdlabs/grid-bot-support/discussions/13.");
                                    return;
#endif
                                }
                                else
                                {
                                    Logger.Singleton.Warning("The first parameter of the command was " +
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
                                        if (!username.IsMatch(GoodUsernameRegex))
                                        {
                                            Logger.Singleton.Warning("Invalid username '{0}'", username);

                                            _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                                            _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond.Increment();

                                            failure = true;

                                            message.Reply("The username you presented contains invalid charcters!");
                                            return;
                                        }

                                        Logger.Singleton.Debug("Trying to get the ID of the user by this username '{0}'", username);
                                        var nullableUserId = UserUtility.GetUserIdByUsername(username);

                                        if (!nullableUserId.HasValue)
                                        {
                                            _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccount.Increment();
                                            _perfmon.TotalItemsProcessedThatHadUsernamesThatDidNotCorrespondToAnAccountPerSecond.Increment();
                                            failure = true;

                                            Logger.Singleton.Warning("The ID for the user '{0}' was null, they were either banned or do not exist.", username);
                                            message.Reply($"The user by the username of '{username}' was not found.");
                                            return;
                                        }

                                        Logger.Singleton.Info("The ID for the user '{0}' was {1}.", username, nullableUserId.Value);
                                        userId = nullableUserId.Value;
                                    }
                                    else
                                    {
                                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond.Increment();
                                        failure = true;

                                        Logger.Singleton.Warning("The user's input username was null or empty, they clearly do not know how to input text.");
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

                                    Logger.Singleton.Warning(
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
                            Logger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                            var user = userId == default ? username : userId.ToString();
                            message.Reply($"The user '{user}' is banned or does not exist.");
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
                Logger.Singleton.Debug("Took {0}s to execute render command.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (failure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.RenderCommandFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
                else
                {
                    _perfmon.RenderCommandSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }
    }
}
