namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;
using Discord.Interactions;

using Prometheus;

using Utility;
using Extensions;

/// <summary>
/// Event handler for interactions.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnInteraction"/>.
/// </remarks>
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
public class OnInteraction(
    MaintenanceSettings maintenanceSettings,
    DiscordShardedClient client,
    InteractionService interactionService,
    IServiceProvider services,
    IAdminUtility adminUtility,
    ILoggerFactory loggerFactory
)
{
    private readonly MaintenanceSettings _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));

    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly InteractionService _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
    private readonly ILoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly Counter _totalInteractionsProcessed = Metrics.CreateCounter(
        "grid_interactions_processed_total",
        "The total number of interactions processed.",
        "interaction_type"
    );

    private readonly Counter _totalInteractionsFailedDueToMaintenance = Metrics.CreateCounter(
        "grid_interactions_failed_due_to_maintenance_total",
        "The total number of interactions failed due to maintenance.",
        "interaction_type"
    );

    private readonly Counter _totalBlacklistedUserAttemptedInteractions = Metrics.CreateCounter(
        "grid_blacklisted_user_attempted_interactions_total",
        "The total number of interactions attempted by blacklisted users.",
        "interaction_user_id",
        "interaction_channel_id",
        "interaction_guild_id"
    );

    private readonly Counter _totalUsersBypassedMaintenance = Metrics.CreateCounter(
        "grid_users_bypassed_maintenance_total",
        "The total number of users that bypassed maintenance.",
        "interaction_user_id",
        "interaction_channel_id",
        "interaction_guild_id"
    );

    private readonly Histogram _interactionProcessingTime = Metrics.CreateHistogram(
        "grid_interaction_processing_time_seconds",
        "The time it takes to process an interaction.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10),
            LabelNames = ["interaction_type"]
        }
    );

    private string GetGuildId(SocketInteraction interaction)
        => interaction.GetGuild(_client).ToString() ?? "DM";

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    public async Task Invoke(SocketInteraction interaction)
    {
        if (interaction.User.IsBot) return;

        _totalInteractionsProcessed.WithLabels(
            interaction.Type.ToString()
        ).Inc();

        await interaction.DeferAsync();

        using var logger = _loggerFactory.CreateLogger(interaction);

        var userIsAdmin = _adminUtility.UserIsAdmin(interaction.User);
        var userIsPrivilaged = _adminUtility.UserIsPrivilaged(interaction.User);
        var userIsBlacklisted = _adminUtility.UserIsBlacklisted(interaction.User);

        if (_maintenanceSettings.MaintenanceEnabled)
        {
            if (!userIsAdmin && !userIsPrivilaged)
            {
                _totalInteractionsFailedDueToMaintenance.WithLabels(
                    interaction.Type.ToString()
                ).Inc();

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

            _totalUsersBypassedMaintenance.WithLabels(
                interaction.User.Id.ToString(),
                interaction.GetChannelAsString(),
                GetGuildId(interaction)
            ).Inc();
        }

        if (userIsBlacklisted)
        {
            _totalBlacklistedUserAttemptedInteractions.WithLabels(
                interaction.User.Id.ToString(),
                interaction.GetChannelAsString(),
                GetGuildId(interaction)
            ).Inc();

            logger.Warning("Blacklisted user tried to use the bot.");

            await interaction.FollowupAsync(
                "You are blacklisted from using the bot, please contact the bot owner for more information."
            );

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

        using var _ = _interactionProcessingTime
            .WithLabels(
                interaction.Type.ToString()
            )
            .NewTimer();

        var context = new ShardedInteractionContext(
            _client,
            interaction
        );

        await _interactionService.ExecuteCommandAsync(
            context,
            _services
        ).ConfigureAwait(false);
    }
}
