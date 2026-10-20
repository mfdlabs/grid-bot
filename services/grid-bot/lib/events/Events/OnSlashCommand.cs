namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;
using Discord.Interactions;

using Microsoft.Extensions.Options;

using Prometheus;
using Grid.Bot.Metrics;

using Utility;
using Extensions;

/// <summary>
/// Event handler for interactions.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnInteraction"/>.
/// </remarks>
/// <param name="maintenanceOptions">The <see cref="MaintenanceOptions"/>.</param>
/// <param name="client">The <see cref="DiscordShardedClient"/>.</param>
/// <param name="interactionService">The <see cref="InteractionService"/>.</param>
/// <param name="services">The <see cref="IServiceProvider"/>.</param>
/// <param name="adminUtility">The <see cref="IAdminUtility"/>.</param>
/// <param name="loggerFactory">The <see cref="IPerUserContextLoggerFactory"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="maintenanceOptions"/> cannot be null.
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="interactionService"/> cannot be null.
/// - <paramref name="services"/> cannot be null.
/// - <paramref name="adminUtility"/> cannot be null.
/// - <paramref name="loggerFactory"/> cannot be null.
/// </exception>
public class OnInteraction(
    IOptionsMonitor<MaintenanceOptions> maintenanceOptions,
    DiscordShardedClient client,
    InteractionService interactionService,
    IServiceProvider services,
    IAdminUtility adminUtility,
    IPerUserContextLoggerFactory loggerFactory
)
{
    private readonly IOptionsMonitor<MaintenanceOptions> _maintenanceOptions = maintenanceOptions ?? throw new ArgumentNullException(nameof(maintenanceOptions));

    private MaintenanceOptions _maintenanceSettings => _maintenanceOptions.CurrentValue;

    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly InteractionService _interactionService = interactionService ?? throw new ArgumentNullException(nameof(interactionService));
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));
    private readonly IAdminUtility _adminUtility = adminUtility ?? throw new ArgumentNullException(nameof(adminUtility));
    private readonly IPerUserContextLoggerFactory _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    private readonly Counter _totalInteractionsProcessed = GridBotMetrics.Interactions.Processed;
    private readonly Counter _totalInteractionsFailedDueToMaintenance = GridBotMetrics.Interactions.FailedDueToMaintenance;
    private readonly Counter _totalBlacklistedUserAttemptedInteractions = GridBotMetrics.Interactions.BlacklistedUserAttempts;
    private readonly Counter _totalUsersBypassedMaintenance = GridBotMetrics.Interactions.UsersBypassingMaintenance;
    private readonly Histogram _interactionProcessingTime = GridBotMetrics.Interactions.ProcessingTime;

    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    public async Task Invoke(SocketInteraction interaction)
    {
        if (interaction.User.IsBot) return;

        var commandName = interaction.Id.ToString();
        var metricCommandName = "unknown"; // Only registered command names are safe to use as a label.

        switch (interaction)
        {
            case SocketSlashCommand slashCommand:
                commandName = metricCommandName = slashCommand.CommandName;
                break;
            case SocketUserCommand userCommand:
                commandName = metricCommandName = userCommand.CommandName;
                break;
            case SocketMessageCommand messageCommand:
                commandName = metricCommandName = messageCommand.CommandName;
                break;
        }

        _totalInteractionsProcessed.WithLabels(
            interaction.Type.ToString(),
            commandName
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
                _totalInteractionsFailedDueToMaintenance.Inc();
                GridBotMetrics.Interactions.Executions.WithLabels(interaction.Type.ToString(), metricCommandName, MetricOutcomes.Rejected).Inc();

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

            _totalUsersBypassedMaintenance.WithLabels(interaction.User.Id.ToString()).Inc();
        }

        if (userIsBlacklisted)
        {
            _totalBlacklistedUserAttemptedInteractions.WithLabels(
                interaction.User.Id.ToString()
            ).Inc();
            GridBotMetrics.Interactions.Executions.WithLabels(interaction.Type.ToString(), metricCommandName, MetricOutcomes.Rejected).Inc();

            logger.Warning("Blacklisted user tried to use the bot.");

            await interaction.FollowupAsync(
                "You are blacklisted from using the bot, please contact the bot owner for more information."
            );

            return;
        }

        logger.Debug("Executing command '{0}'.", commandName);

        InteractionExecutionTracker.Start(interaction, interaction.Type.ToString(), metricCommandName);

        Task.Run(async () =>
        {
            // This only times the dispatch, interactions run asynchronously. Kept for dashboard compatibility,
            // gridbot_interaction_duration_seconds measures the actual execution.
            using var _ = _interactionProcessingTime
                .WithLabels(
                    interaction.Type.ToString(),
                    commandName
                )
                .NewTimer();

            var context = new ShardedInteractionContext(
                _client,
                interaction
            );

            try
            {
                var result = await _interactionService.ExecuteCommandAsync(
                    context,
                    _services
                ).ConfigureAwait(false);

                // Successful results complete when the interaction-executed event fires.
                if (!result.IsSuccess)
                    InteractionExecutionTracker.Complete(interaction, OutcomeClassifier.FromInteractionResult(result));
            }
            catch
            {
                InteractionExecutionTracker.Complete(interaction, MetricOutcomes.Exception);

                throw;
            }
        });
    }
}
