using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord.WebSocket;

using Logging;

using Diagnostics;
using Text.Extensions;
using Instrumentation;

using Grid.Bot.Utility;
using Grid.Bot.Interfaces;
using Grid.Bot.Extensions;
using Grid.Bot.PerformanceMonitors;

namespace Grid.Bot.Commands
{
    internal class Render : IStateSpecificCommandHandler
    {
        public string CommandName => "Render User";
        public string CommandDescription => $"Renders a Roblox user!\nLayout: " +
                                            $"{Grid.Bot.Properties.Settings.Default.Prefix}render " +
                                            $"robloxUserID?|...userName?";
        public string[] CommandAliases => new[] { "r", "render" };
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;

        private sealed class RenderCommandPerformanceMonitor
        {
            private const string Category = "Grid.Commands.Render";

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
                (from uname in global::Grid.Bot.Properties.Settings.Default.BlacklistedUsernamesForRendering.Split(',')
                 where !uname.IsNullOrEmpty()
                 select uname).ToArray();

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
                using (message.Channel.EnterTypingState())
                {
                    var userIsAdmin = message.Author.IsAdmin();

                    if (FloodCheckerRegistry.RenderFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
                    {
                        message.Reply("Too many people are using this command at once, please wait a few moments and try again.");
                        return;
                    }

                    FloodCheckerRegistry.RenderFloodChecker.UpdateCount();

                    var perUserFloodChecker = FloodCheckerRegistry.GetPerUserRenderFloodChecker(message.Author.Id);
                    if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
                    {
                        message.Reply("You are sending render commands too quickly, please wait a few moments and try again.");
                        return;
                    }

                    perUserFloodChecker.UpdateCount();

                    string username = null;

                    if (!long.TryParse(contentArray.ElementAtOrDefault(0), out var userId))
                    {
                        _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();

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

                            Logger.Singleton.Information("The ID for the user '{0}' was {1}.", username, nullableUserId.Value);
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
                                          $"{Grid.Bot.Properties.Settings.Default.Prefix}{originalCommandName} " +
                                          $"userID|userName");
                            return;
                        }
                    }
                    else
                    {
                        if (userId > global::Grid.Bot.Properties.Settings.Default.MaxUserIDSize)
                        {
                            _perfmon.TotalItemsProcessedThatHadInvalidUserIDs.Increment();
                            _perfmon.TotalItemsProcessedThatHadInvalidUserIDsPerSecond.Increment();
                            failure = true;

                            Logger.Singleton.Warning(
                                "The input user ID of {0} was greater than the environment's maximum user ID size of {1}.",
                                userId,
                                global::Grid.Bot.Properties.Settings.Default.MaxUserIDSize
                            );
                            message.Reply($"The userId '{userId}' is too big, expected the " +
                                          $"userId to be less than or equal to " +
                                          $"'{Grid.Bot.Properties.Settings.Default.MaxUserIDSize}'");
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

                    Logger.Singleton.Information(
                        "Trying to render the character for the user '{0}' with the place '{1}', " +
                        "and the dimensions of {2}x{3}",
                        userId,
                        global::Grid.Bot.Properties.Settings.Default.RenderPlaceID,
                        global::Grid.Bot.Properties.Settings.Default.RenderSizeX,
                        global::Grid.Bot.Properties.Settings.Default.RenderSizeY
                    );

                    // get a stream and temp filename
                    var (stream, fileName) = GridServerCommandUtility.RenderUser(
                        userId,
                        global::Grid.Bot.Properties.Settings.Default.RenderPlaceID,
                        global::Grid.Bot.Properties.Settings.Default.RenderSizeX,
                        global::Grid.Bot.Properties.Settings.Default.RenderSizeY
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
            finally
            {
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
