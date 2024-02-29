namespace Grid.Bot.Events;

using System;
using System.Linq;
using System.Threading.Tasks;
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
/// Construct a new instance of <see cref="OnMessage"/>.
/// </remarks>
/// <param name="commandsSettings">The <see cref="CommandsSettings"/>.</param>
/// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="commandsSettings"/> cannot be null.
/// - <paramref name="maintenanceSettings"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// - <paramref name="loggerFactory"/> cannot be null.
/// </exception>
public partial class OnMessage(
    CommandsSettings commandsSettings,
    MaintenanceSettings maintenanceSettings,
    DiscordShardedClient client,
    IAdminUtility adminUtility,
    ILoggerFactory loggerFactory
)
{
    // language=regex
    private const string _allowedCommandRegex = @"^[a-zA-Z-]*$";

    [GeneratedRegex(_allowedCommandRegex)]
    private static partial Regex GetAllowedCommandRegex();

    private readonly CommandsSettings _commandsSettings = commandsSettings ?? throw new ArgumentNullException(nameof(commandsSettings));
    private readonly MaintenanceSettings _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));

    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly Counter _totalMessagesProcessed = Metrics.CreateCounter(
        "grid_messages_processed_total",
        "The total number of messages processed."
    );

    private readonly Counter _totalUsersUsingPreviousPhaseCommands = Metrics.CreateCounter(
        "grid_users_using_previous_phase_commands_total",
        "The total number of users using previous phase commands.",
        "command_name"
    );

    private readonly Counter _totalMessagesFailedDueToMaintenance = Metrics.CreateCounter(
        "grid_messages_failed_due_to_maintenance_total",
        "The total number of messages failed due to maintenance."
    );

    private readonly Counter _totalBlacklistedUserAttemptedMessages = Metrics.CreateCounter(
        "grid_blacklisted_user_attempted_messages_total",
        "The total number of messages attempted by blacklisted users.",
        "message_user_id",
        "message_channel_id",
        "message_guild_id"
    );

    private readonly Counter _totalUsersBypassedMaintenance = Metrics.CreateCounter(
        "grid_users_bypassed_maintenance_total",
        "The total number of users that bypassed maintenance.",
        "message_user_id",
        "message_channel_id",
        "message_guild_id"
    );

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

        using var logger = _loggerFactory.CreateLogger(message);

        var userIsAdmin = _adminUtility.UserIsAdmin(message.Author);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(message.Author);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(message.Author);

        int argPos = 0;

        if (!message.HasStringPrefix(_commandsSettings.Prefix, ref argPos)
         && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

        // Get the name of the command that was used.
        var commandName = message.Content.Split(' ')[0];
        if (string.IsNullOrEmpty(commandName)) return;

        // Try catch here temporary until mfdlabs/grid-bot#279 is resolved.
        try
        {
            commandName = commandName[argPos..];
            if (string.IsNullOrEmpty(commandName)) return;
            if (!GetAllowedCommandRegex().IsMatch(commandName)) return;
            if (!_commandsSettings.PreviousPhaseCommands.Contains(commandName.ToLowerInvariant())) return;
        }
        catch (ArgumentException ex)
        {
            logger.Error("Failed to get the command name because: {0}, Raw Text: {1}", ex, message.Content);
            return;
        }

        _totalUsersUsingPreviousPhaseCommands.WithLabels(
            commandName
        ).Inc();

        logger.Warning(
            "User tried to use previous phase command '{0}', but it is no longer supported.",
            commandName
        );

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
                message.Channel.Id.ToString(),
                GetGuildId(message)
            ).Inc();
        }

        if (userIsBlacklisted)
        {
            _totalBlacklistedUserAttemptedMessages.WithLabels(
                message.Author.Id.ToString(),
                message.Channel.Id.ToString(),
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

        await message.ReplyAsync("Text commands are no longer supported and will be permanently removed in the future, please use slash commands instead.");
    }
}
