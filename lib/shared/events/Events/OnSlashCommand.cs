namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord.WebSocket;
using Discord.Interactions;

using Random;
using Logging;

using Utility;
using Discord;

/// <summary>
/// Event handler for interactions.
/// </summary>
public class OnInteraction
{
    private readonly DiscordSettings _discordSettings;
    private readonly MaintenanceSettings _maintenanceSettings;
    private readonly DiscordRolesSettings _discordRolesSettings;

    private readonly ILogger _logger;
    private readonly DiscordShardedClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly IAdminUtility _adminUtility;
    private readonly IRandom _random;

    private DiscordSocketClient GetShard()
        => _client.GetShard(_random.Next(_client.Shards.Count));

    /// <summary>
    /// Construct a new instance of <see cref="OnInteraction"/>.
    /// </summary>
    /// <param name="discordSettings">The <see cref="DiscordSettings"/>.</param>
    /// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/>.</param>
    /// <param name="discordRolesSettings">The <see cref="DiscordRolesSettings"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
    /// <param name="interactionService">The <see cref="InteractionService"/>.</param>
    /// <param name="services">The <see cref="IServiceProvider"/>.</param>
    /// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
    /// <param name="random">The <see cref="IRandom"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="maintenanceSettings"/> cannot be null.
    /// - <paramref name="discordRolesSettings"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="client"/> cannot be null.
    /// - <paramref name="interactionService"/> cannot be null.
    /// - <paramref name="services"/> cannot be null.
    /// - <paramref name="adminUtility"/> cannot be null.
    /// - <paramref name="random"/> cannot be null.
    /// </exception>
    public OnInteraction(
        DiscordSettings discordSettings,
        MaintenanceSettings maintenanceSettings,
        DiscordRolesSettings discordRolesSettings,
        ILogger logger,
        DiscordShardedClient client,
        InteractionService interactionService,
        IServiceProvider services,
        IAdminUtility adminUtility,
        IRandom random
    )
    {
        _discordSettings = discordSettings ?? throw new ArgumentNullException(nameof(discordSettings));
        _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));
        _discordRolesSettings = discordRolesSettings ?? throw new ArgumentNullException(nameof(discordRolesSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    public async Task Invoke(SocketInteraction interaction)
    {
        if (interaction.User.IsBot) return;

        await interaction.DeferAsync();

        var userIsAdmin = _adminUtility.UserIsAdmin(interaction.User);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(interaction.User);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(interaction.User);

        // Check if the commands requested by the interaction exists.
#if DEBUG

        SocketApplicationCommand actualCommand = null;

        if (_discordSettings.DebugGuildId != 0UL)
        {
            var guild = _client.GetGuild(_discordSettings.DebugGuildId);
            actualCommand = await guild.GetApplicationCommandAsync(interaction.Id);
        }
#else
        var actualCommand = await GetShard().GetGlobalApplicationCommandAsync(interaction.Id);
#endif

        switch (interaction)
        {
            case SocketSlashCommand slashCommand:
                if (!_interactionService.SearchSlashCommand(slashCommand).IsSuccess)
                {
                    _logger.Warning(
                        "User {0}('{1}#{2}') tried to use the command '{3}', but it does not exist.",
                        interaction.User.Id,
                        interaction.User.Username,
                        interaction.User.Discriminator,
                        slashCommand.CommandName
                    );

                    await interaction.DeleteOriginalResponseAsync();

                    // Delete the slash command if it exists.
                    if (actualCommand != null)
                        await actualCommand.DeleteAsync();

                    return;
                }

                break;
            case SocketMessageComponent messageComponent:
                if (!_interactionService.SearchComponentCommand(messageComponent).IsSuccess)
                {
                    _logger.Warning(
                        "User {0}('{1}#{2}') tried to use the message component '{3}', but it does not exist.",
                        interaction.User.Id,
                        interaction.User.Username,
                        interaction.User.Discriminator,
                        messageComponent.Id
                    );

                    await interaction.DeleteOriginalResponseAsync();

                    if (actualCommand != null)
                        await actualCommand.DeleteAsync();

                    return;
                }

                break;
            case SocketUserCommand userCommand:
                if (!_interactionService.SearchUserCommand(userCommand).IsSuccess)
                {
                    _logger.Warning(
                        "User {0}('{1}#{2}') tried to use the user command '{3}', but it does not exist.",
                        interaction.User.Id,
                        interaction.User.Username,
                        interaction.User.Discriminator,
                        userCommand.CommandName
                    );

                    await interaction.DeleteOriginalResponseAsync();

                    if (actualCommand != null)
                        await actualCommand.DeleteAsync();

                    return;
                }

                break;

            case SocketMessageCommand messageCommand:
                if (!_interactionService.SearchMessageCommand(messageCommand).IsSuccess)
                {
                    _logger.Warning(
                        "User {0}('{1}#{2}') tried to use the message command '{3}', but it does not exist.",
                        interaction.User.Id,
                        interaction.User.Username,
                        interaction.User.Discriminator,
                        messageCommand.CommandName
                    );

                    await interaction.DeleteOriginalResponseAsync();

                    if (actualCommand != null)
                        await actualCommand.DeleteAsync();

                    return;
                }

                break;

            case SocketAutocompleteInteraction autocompleteInteraction:
                if (!_interactionService.SearchAutocompleteCommand(autocompleteInteraction).IsSuccess)
                {
                    _logger.Warning(
                        "User {0}('{1}#{2}') tried to use the autocomplete command '{3}', but it does not exist.",
                        interaction.User.Id,
                        interaction.User.Username,
                        interaction.User.Discriminator,
                        autocompleteInteraction.Id
                    );

                    await interaction.DeleteOriginalResponseAsync();

                    return;
                }

                break;
        }

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

                _logger.Warning(
                    "Maintenance enabled user ({0}('{1}#{2}')) tried to use the bot, in channel {3}({4}) in guild {5}({6}).",
                    interaction.User.Id,
                    interaction.User.Username,
                    interaction.User.Discriminator,
                    interaction.Channel.Id,
                    interaction.Channel.Name,
                    guildId,
                    guildName
                );

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
            _logger.Warning(
                "A blacklisted user {0}('{1}#{2}') tried to use the bot, attempt to DM that they are blacklisted.",
                interaction.User.Id,
                interaction.User.Username,
                interaction.User.Discriminator
            );

            try
            {
                var dmChannel = await interaction.User.CreateDMChannelAsync();

                await dmChannel?.SendMessageAsync(
                    "You are blacklisted from using the bot, please contact the bot owner for more information."
                );
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Failed to DM blacklisted user {0}('{1}#{2}'): {3}",
                    interaction.User.Id,
                    interaction.User.Username,
                    interaction.User.Discriminator,
                    ex
                );
            }

            return;
        }

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
