﻿#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Discord;
using Discord.WebSocket;
using MFDLabs.Logging;
using MFDLabs.Threading;
using MFDLabs.Diagnostics;
using MFDLabs.Instrumentation;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.Bot.Utility;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Reflection.Extensions;
using MFDLabs.Grid.Bot.PerformanceMonitors;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal class Render : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Renders a roblox user.";
        public string CommandAlias => "render";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("roblox_id")
                .WithDescription("Render a user by their Roblox User ID.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "The user ID of the Roblox user.", true, minValue: 1, maxValue: global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize),
            new SlashCommandOptionBuilder()
                .WithName("roblox_name")
                .WithDescription("Render a user by their Roblox Username.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user_name", ApplicationCommandOptionType.String, "The user name of the Roblox user.", true),
            new SlashCommandOptionBuilder()
                .WithName("discord_user")
                .WithDescription("Render a user by their Discord Account.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "The user ref to render.", true),
            new SlashCommandOptionBuilder()
                .WithName("self")
                .WithDescription("Render yourself!")
                .WithType(ApplicationCommandOptionType.SubCommand),
        };
        private sealed class RenderSlashCommandPerformanceMonitor
        {
            private const string Category = "MFDLabs.Grid.SlashCommands.Render";

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

#if FEATURE_RBXDISCORDUSERS_CLIENT

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

#else
                    item.RespondEphemeralPing("Calling the render command like this is deprecated until further notice. Please see https://github.com/mfdlabs/grid-bot-support/discussions/13.");
                    return long.MinValue;
#endif

                case "self":

#if FEATURE_RBXDISCORDUSERS_CLIENT

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

#else
                    item.RespondEphemeralPing("Calling the render command like this is deprecated until further notice. Please see https://github.com/mfdlabs/grid-bot-support/discussions/13.");
                    return long.MinValue;
#endif
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
                _itemCount++;

                if (_processingItem)
                    await command.RespondEphemeralPingAsync($"The render queue is currently trying to process {_itemCount} items, please wait for your result to be processed.");

                lock (_renderLock)
                {
                    _processingItem = true;

                    var subCommand = command.Data.GetSubCommand();

                    var userId = GetUserId(ref command, ref subCommand, ref failure);
                    if (userId == long.MinValue) return;

                    if (UserUtility.GetIsUserBanned(userId))
                    {
                        Logger.Singleton.Warning("The input user ID of {0} was linked to a banned user account.", userId);
                        command.RespondEphemeralPing($"The user '{userId}' is banned or does not exist.");
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
                        command.RespondEphemeralPing("Internal failure when processing item render.");

                        return;
                    }

                    using (stream)
                        command.RespondWithFilePublicPing(
                            stream,
                            fileName
                        );
                }
            }
            finally
            {
                _itemCount--;
                _processingItem = false;
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