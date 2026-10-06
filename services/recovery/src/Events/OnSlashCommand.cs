namespace Grid.Bot.Events;

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.Options;

using Prometheus;

using Logging;

/// <summary>
/// Event handler for interactions.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="OnInteraction"/>.
/// </remarks>
/// <param name="options">The <see cref="RecoveryOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="options"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// </exception>
public class OnInteraction(
    IOptions<RecoveryOptions> options,
    ILogger logger
)
{
    private readonly RecoveryOptions _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly Counter _totalInteractionsProcessed = Metrics.CreateCounter(
        "grid_interactions_processed_total",
        "The total number of interactions processed.",
        "interaction_type"
    );

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

        _logger.Information(
            "User tried to use interaction '{0}'",
            interaction.ToString()
        );

        await interaction.DeferAsync();

        var failureMessage = _settings.MaintenanceStatusMessage;

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
