namespace Grid.Bot.Events;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if DEBUG
using System.Reflection;
#endif

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Prometheus;

using Utility;

/// <summary>
/// Event handler for messages.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OnMessage"/> class.
/// </remarks>
/// <param name="commandsSettings">The <see cref="CommandsSettings"/>.</param>
/// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
/// <param name="commandService">The <see cref="CommandService"/>.</param>
/// <param name="discordClient">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="commandsSettings"/> cannot be null.
/// - <paramref name="maintenanceSettings"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// - <paramref name="loggerFactory"/> cannot be null.
/// - <paramref name="commandService"/> cannot be null.
/// - <paramref name="discordClient"/> cannot be null.
/// - <paramref name="services"/> cannot be null.
/// </exception>
public partial class OnMessage(
    CommandsSettings commandsSettings,
    MaintenanceSettings maintenanceSettings,
    IAdminUtility adminUtility,
    ILoggerFactory loggerFactory,
    CommandService commandService,
    DiscordShardedClient discordClient,
    IServiceProvider services
)
{
    // language=regex
    private const string _allowedCommandRegex = @"^[a-zA-Z-_]*$";

    [GeneratedRegex(_allowedCommandRegex, RegexOptions.Singleline)]
    private static partial Regex GetAllowedCommandRegex();

    private readonly CommandsSettings _commandsSettings = commandsSettings ?? throw new ArgumentNullException(nameof(commandsSettings));
    private readonly MaintenanceSettings _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));

    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly CommandService _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
    private readonly DiscordShardedClient _discordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    private readonly HashSet<string> _commandAliases = []; // Used to filter out commands that are not meant for the bot.

    private readonly Counter _totalMessagesProcessed = Metrics.CreateCounter(
        "bot_messages_processed_total",
        "The total number of messages processed."
    );

    private readonly Counter _totalCommandsProcessed = Metrics.CreateCounter(
        "bot_commands_processed_total",
        "The total number of commands processed.",
        "command_name"
    );

    private readonly Counter _totalMessagesFailedDueToMaintenance = Metrics.CreateCounter(
        "bot_messages_failed_due_to_maintenance_total",
        "The total number of messages failed due to maintenance."
    );

    private readonly Counter _totalBlacklistedUserAttemptedMessages = Metrics.CreateCounter(
        "bot_blacklisted_user_attempted_messages_total",
        "The total number of messages attempted by blacklisted users.",
        "message_user_id",
        "message_channel_id",
        "message_guild_id"
    );

    private readonly Counter _totalUsersBypassedMaintenance = Metrics.CreateCounter(
        "bot_users_bypassed_maintenance_total",
        "The total number of users that bypassed maintenance.",
        "message_user_id",
        "message_channel_id",
        "message_guild_id"
    );

    private readonly Histogram _commandProcessingTime = Metrics.CreateHistogram(
        "bot_command_processing_time_seconds",
        "The time it takes to process an command.",
        "command_name"
    );

    /// <summary>
    /// Initialize the event handler.
    /// </summary>
    public void Initialize()
    {
        var modules = _commandService.Modules;

        _commandAliases.Clear();

        _commandAliases.UnionWith(
            modules.SelectMany(x => x.Aliases)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.ToLowerInvariant())
        );

        _commandAliases.UnionWith(
            modules.SelectMany(x => x.Commands)
                .SelectMany(x => x.Aliases)
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x.ToLowerInvariant())
                .Select(x => x.Split(' ').First())
        );
    }

    private static string GetGuildId(SocketMessage message)
    {
        if (message.Channel is SocketGuildChannel guildChannel)
            return guildChannel.Guild.Id.ToString();

        return "DM";
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="rawMessage">The <see cref="SocketMessage"/></param>
    public async Task Invoke(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        _totalMessagesProcessed.Inc();

        int argPos = 0;

        if (!message.HasStringPrefix(_commandsSettings.Prefix, ref argPos, StringComparison.OrdinalIgnoreCase)) return;

        // Get the name of the command that was used.
        var commandName = message.Content.Split(' ').First();
        if (string.IsNullOrEmpty(commandName)) return;

        commandName = commandName[argPos..];
        if (string.IsNullOrEmpty(commandName)) return;
        if (!GetAllowedCommandRegex().IsMatch(commandName)) return;
        if (!_commandAliases.Contains(commandName.ToLowerInvariant())) return;

        using var logger = _loggerFactory.CreateLogger(message);

        var userIsAdmin = _adminUtility.UserIsAdmin(message.Author);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(message.Author);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(message.Author);

        _totalCommandsProcessed.WithLabels(
            commandName
        ).Inc();

#if DEBUG

        var entryAssembly = Assembly.GetEntryAssembly();
        var informationalVersion = entryAssembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrEmpty(informationalVersion))
            informationalVersion = entryAssembly?.GetName().Version?.ToString();

        if (!string.IsNullOrEmpty(informationalVersion))
            await message.Channel.SendMessageAsync($"Debug build running version {informationalVersion}.");

#endif

        if (_maintenanceSettings.MaintenanceEnabled)
        {
            if (!userIsAdmin && !userIsPrivilaged)
            {
                _totalMessagesFailedDueToMaintenance.Inc();

                logger.Warning(
                    "User tried to use the command '{0}', but maintenance is enabled.",
                    commandName
                );

                var failureMessage = _maintenanceSettings.MaintenanceStatus;

                var embed = new EmbedBuilder()
                    .WithTitle("Maintenance Enabled")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                if (!string.IsNullOrEmpty(failureMessage))
                    embed.WithDescription(failureMessage);

                await message.ReplyAsync("Maintenance is currently enabled, please try again later.", embeds: [embed.Build()]);

                return;
            }

            _totalUsersBypassedMaintenance.WithLabels(
                message.Author.Id.ToString(),
                message.Channel?.Id.ToString() ?? message.Thread?.Id.ToString(),
                GetGuildId(message)
            ).Inc();
        }

        if (userIsBlacklisted)
        {
            _totalBlacklistedUserAttemptedMessages.WithLabels(
                message.Author.Id.ToString(),
                message.Channel?.Id.ToString() ?? message.Thread?.Id.ToString(),
                GetGuildId(message)
            ).Inc();

            logger.Warning("Blacklisted user tried to use the bot.");

            try
            {
                var dmChannel = await message.Author.CreateDMChannelAsync();

                await dmChannel?.SendMessageAsync(
                    "You are blacklisted from using the bot, please contact the bot owner for more information."
                );
            }
            catch (Exception ex)
            {
                logger.Error("Failed to send blacklisted user a DM because: {0}", ex);
            }

            return;
        }

        if (message.Content.Contains("@everyone", StringComparison.CurrentCultureIgnoreCase) ||
            message.Content.Contains("@here", StringComparison.CurrentCultureIgnoreCase) &&
            !userIsAdmin)
            return;

        if (!_commandsSettings.EnableTextCommands)
        {
            if (!_commandsSettings.ShouldWarnWhenCommandsAreDisabled)
                return;

            if (string.IsNullOrEmpty(_commandsSettings.TextCommandsDisabledWarningText))
                await message.ReplyAsync("Text commands are disabled, please use slash commands instead.");
            else
                await message.ReplyAsync(_commandsSettings.TextCommandsDisabledWarningText);

            return;
        }

        Task.Run(async () =>
        {
            using var _ = _commandProcessingTime.WithLabels(commandName).NewTimer();

            var context = new ShardedCommandContext(_discordClient, message);

            await _commandService.ExecuteAsync(context, argPos, _services).ConfigureAwait(false);
        });
    }
}
