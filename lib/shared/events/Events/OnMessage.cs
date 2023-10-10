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

using Utility;

/// <summary>
/// Event handler for messages.
/// </summary>
public class OnMessage
{
    // language=regex
    private const string _allowedCommandRegex = @"^[a-zA-Z-]*$";

    private readonly CommandsSettings _commandsSettings;
    private readonly MaintenanceSettings _maintenanceSettings;

    private readonly DiscordShardedClient _client;
    private readonly IAdminUtility _adminUtility;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Construct a new instance of <see cref="OnMessage"/>.
    /// </summary>
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
    public OnMessage(
        CommandsSettings commandsSettings,
        MaintenanceSettings maintenanceSettings,
        DiscordShardedClient client,
        IAdminUtility adminUtility,
        ILoggerFactory loggerFactory
    )
    {
        _commandsSettings = commandsSettings ?? throw new ArgumentNullException(nameof(commandsSettings));
        _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="rawMessage">The <see cref="SocketMessage"/></param>
    public async Task Invoke(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return;
        if (message.Author.IsBot) return;

        using var logger = _loggerFactory.CreateLogger(message);

        var userIsAdmin = _adminUtility.UserIsAdmin(message.Author);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(message.Author);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(message.Author);

        int argPos = 0;

        if (!message.HasStringPrefix(_commandsSettings.Prefix, ref argPos)
         && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

        // Get the name of the command that was used.
        var commandName = message.Content.Split(' ')[0][argPos..];
        if (string.IsNullOrEmpty(commandName)) return;
        if (!Regex.IsMatch(commandName, _allowedCommandRegex)) return;
        if (!_commandsSettings.PreviousPhaseCommands.Contains(commandName.ToLowerInvariant())) return;

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
                var guildName = string.Empty;
                var guildId = 0UL;

                if (message.Channel is SocketGuildChannel guildChannel)
                {
                    guildName = guildChannel.Guild.Name;
                    guildId = guildChannel.Guild.Id;
                }

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

                await message.ReplyAsync("Maintenance is currently enabled, please try again later.", embeds: new[] { embed.Build() });

                return;
            }
        }

        if (userIsBlacklisted)
        {
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

        if (message.Content.ToLower().Contains("@everyone") || message.Content.ToLower().Contains("@here") && !userIsAdmin)
            return;



        await message.ReplyAsync("Text commands are no longer supported and will be permanently removed in the future, please use slash commands instead.");
    }
}
