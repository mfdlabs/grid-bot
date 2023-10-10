namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord.WebSocket;
using Discord.Interactions;

using Utility;
using Discord;

/// <summary>
/// Event handler for interactions.
/// </summary>
public class OnInteraction
{
    private readonly DiscordSettings _discordSettings;
    private readonly MaintenanceSettings _maintenanceSettings;

    private readonly DiscordShardedClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly IAdminUtility _adminUtility;
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Construct a new instance of <see cref="OnInteraction"/>.
    /// </summary>
    /// <param name="discordSettings">The <see cref="DiscordSettings"/>.</param>
    /// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/>.</param>
    /// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
    /// <param name="interactionService">The <see cref="InteractionService"/>.</param>
    /// <param name="services">The <see cref="IServiceProvider"/>.</param>
    /// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
    /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="maintenanceSettings"/> cannot be null.
    /// - <paramref name="client"/> cannot be null.
    /// - <paramref name="interactionService"/> cannot be null.
    /// - <paramref name="services"/> cannot be null.
    /// - <paramref name="adminUtility"/> cannot be null.
    /// - <paramref name="loggerFactory"/> cannot be null.
    /// </exception>
    public OnInteraction(
        DiscordSettings discordSettings,
        MaintenanceSettings maintenanceSettings,
        DiscordShardedClient client,
        InteractionService interactionService,
        IServiceProvider services,
        IAdminUtility adminUtility,
        ILoggerFactory loggerFactory
    )
    {
        _discordSettings = discordSettings ?? throw new ArgumentNullException(nameof(discordSettings));
        _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    public async Task Invoke(SocketInteraction interaction)
    {
        if (interaction.User.IsBot) return;

        await interaction.DeferAsync();

        using var logger = _loggerFactory.CreateLogger(interaction);

        var userIsAdmin = _adminUtility.UserIsAdmin(interaction.User);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(interaction.User);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(interaction.User);

        if (_maintenanceSettings.MaintenanceEnabled)
        {
            if (!userIsAdmin && !userIsPrivilaged)
            {
                var guildName = string.Empty;
                var guildId = 0UL;

                if (interaction.Channel is SocketGuildChannel guildChannel)
                {
                    guildName = guildChannel.Guild.Name;
                    guildId = guildChannel.Guild.Id;
                }

                logger.Warning("Maintenance enabled user tried to use the bot.");

                var failureMessage = _maintenanceSettings.MaintenanceStatus;

                var builder = new EmbedBuilder()
                    .WithTitle("Maintenance Enabled")
                    .WithColor(Color.Red)
                    .WithCurrentTimestamp();

                var embeds = new List<Embed>();

                if (!string.IsNullOrEmpty(failureMessage))
                {
                    builder.WithDescription(failureMessage);

                    embeds.Add(builder.Build());
                }

                await interaction.FollowupAsync("Maintenance is currently enabled, please try again later.", embeds: embeds.ToArray());

                return;
            }
        }

        if (userIsBlacklisted)
        {
            logger.Warning("Blacklisted user tried to use the bot.");

            try
            {
                var dmChannel = await interaction.User.CreateDMChannelAsync();

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

        var commandName = interaction.Id.ToString();

        switch (interaction)
        {
            case SocketSlashCommand slashCommand:
                commandName = slashCommand.CommandName;
                break;
            case SocketUserCommand userCommand:
                commandName = userCommand.CommandName;
                break;
            case SocketMessageCommand messageCommand:
                commandName = messageCommand.CommandName;
                break;
        }

        logger.Debug("Executing command '{0}'.", commandName);

        Task.Run(async () =>
        {

            var context = new ShardedInteractionContext(
                _client,
                interaction
            );

            await _interactionService.ExecuteCommandAsync(
                context,
                _services
            ).ConfigureAwait(false);
        });
    }
}
