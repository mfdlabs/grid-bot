#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Logging;

using Diagnostics;
using Instrumentation;
using Text.Extensions;
using Reflection.Extensions;

using Grid.Bot.Utility;
using Grid.Bot.Interfaces;
using Grid.Bot.Extensions;
using Grid.Bot.PerformanceMonitors;

namespace Grid.Bot.SlashCommands
{
    internal class Render : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Renders a roblox user.";
        public string Name => "render";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("roblox_id")
                .WithDescription("Render a user by their Roblox User ID.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "The user ID of the Roblox user.", true, minValue: 1, maxValue: global::Grid.Bot.Properties.Settings.Default.MaxUserIDSize),
            new SlashCommandOptionBuilder()
                .WithName("roblox_name")
                .WithDescription("Render a user by their Roblox Username.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user_name", ApplicationCommandOptionType.String, "The user name of the Roblox user.", true)
        };
        private sealed class RenderSlashCommandPerformanceMonitor
        {
            private const string Category = "Grid.SlashCommands.Render";

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
            public IAverageValueCounter RenderSlashCommandSuccessAverageTimeTicks { get; }
            public IAverageValueCounter RenderSlashCommandFailureAverageTimeTicks { get; }

            public RenderSlashCommandPerformanceMonitor(ICounterRegistry counterRegistry)
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
                RenderSlashCommandSuccessAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderSlashCommandSuccessAverageTimeTicks", instance);
                RenderSlashCommandFailureAverageTimeTicks = counterRegistry.GetAverageValueCounter(Category, "RenderSlashCommandFailureAverageTimeTicks", instance);
            }
        }

        // language=regex
        private const string GoodUsernameRegex = @"^[A-Za-z0-9_]{3,20}$";

        private static IEnumerable<string> BlacklistedUsernames =>
                (from uname in global::Grid.Bot.Properties.Settings.Default.BlacklistedUsernamesForRendering.Split(',')
                 where !uname.IsNullOrEmpty()
                 select uname).ToArray();

        #region Metrics

        private static readonly RenderSlashCommandPerformanceMonitor _perfmon = new(PerfmonCounterRegistryProvider.Registry);

        #endregion Metrics

        private static long GetUserId(
            ref SocketSlashCommand item,
            ref SocketSlashCommandDataOption subCommand,
            ref bool failure
        )
        {
            var userId = 0L;

            switch (subCommand.Name.ToLower())
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

                    if (!username.IsMatch(GoodUsernameRegex))
                    {
                        Logger.Singleton.Warning("Invalid username '{0}'", username);

                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernames.Increment();
                        _perfmon.TotalItemsProcessedThatHadNullOrEmptyUsernamesPerSecond.Increment();
                        failure = true;

                        item.RespondEphemeralPing("The username you presented contains invalid charcters!");
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

                    Logger.Singleton.Information("The ID for the user '{0}' was {1}.", username, nullableUserIdRemote.Value);
                    userId = nullableUserIdRemote.Value;

                    break;
            }

            return userId;
        }

        public async Task Invoke(SocketSlashCommand command)
        {
            _perfmon.TotalItemsProcessed.Increment();
            _perfmon.TotalItemsProcessedPerSecond.Increment();

            var sw = Stopwatch.StartNew();
            bool failure = false;

            try
            {
                var userIsAdmin = command.User.IsAdmin();

                if (FloodCheckerRegistry.RenderFloodChecker.IsFlooded() && !userIsAdmin) // allow admins to bypass
                {
                    await command.RespondEphemeralAsync("Too many people are using this command at once, please wait a few moments and try again.");
                    return;
                }

                FloodCheckerRegistry.RenderFloodChecker.UpdateCount();

                var perUserFloodChecker = FloodCheckerRegistry.GetPerUserRenderFloodChecker(command.User.Id);
                if (perUserFloodChecker.IsFlooded() && !userIsAdmin)
                {
                    await command.RespondEphemeralAsync("You are sending render commands too quickly, please wait a few moments and try again.");
                    return;
                }

                perUserFloodChecker.UpdateCount();

                var subCommand = command.Data.GetSubCommand();

                var userId = GetUserId(ref command, ref subCommand, ref failure);
                if (userId == long.MinValue) return;

                if (UserUtility.GetIsUserBanned(userId))
                {
                    Logger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                    await command.RespondEphemeralPingAsync($"The user '{userId}' is banned or does not exist.");
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
                    await command.RespondEphemeralPingAsync("Internal failure when processing item render.");

                    return;
                }

                using (stream)
                    await command.RespondWithFilePublicPingAsync(
                        stream,
                        fileName
                    );
            }
            finally
            {
                sw.Stop();
                Logger.Singleton.Debug("Took {0}s to execute render slash command.", sw.Elapsed.TotalSeconds.ToString("f7"));

                if (failure)
                {
                    _perfmon.TotalItemsProcessedThatFailed.Increment();
                    _perfmon.TotalItemsProcessedThatFailedPerSecond.Increment();
                    _perfmon.RenderSlashCommandFailureAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
                else
                {
                    _perfmon.RenderSlashCommandSuccessAverageTimeTicks.Sample(sw.ElapsedTicks);
                }
            }
        }

    }
}

#endif
