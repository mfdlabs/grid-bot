namespace Grid.Bot.Events;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Discord.WebSocket;

using Prometheus;

using Grid.Bot.Metrics;
using Grid.Bot.Telemetry;

/// <summary>
/// Measures and traces an interaction from dispatch to completion.
/// </summary>
/// <remarks>
/// Interactions run asynchronously, so the interaction service returns before the command has finished.
/// The duration is recorded and the span is ended when the interaction-executed event fires instead.
/// </remarks>
internal static class InteractionExecutionTracker
{
    private sealed class Entry(string interactionType, string commandName, IDisposable inFlight, Activity activity)
    {
        public readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        public readonly string InteractionType = interactionType;
        public readonly string CommandName = commandName;
        public readonly IDisposable InFlight = inFlight;
        public readonly Activity Activity = activity;
    }

    private static readonly ConditionalWeakTable<SocketInteraction, Entry> _entries = [];

    /// <summary>
    /// Begins tracking an interaction.
    /// </summary>
    /// <remarks>
    /// Call this from the task that runs the interaction, so the span becomes the parent of the work it starts.
    /// </remarks>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    /// <param name="interactionType">The interaction type.</param>
    /// <param name="commandName">The command name, or <c>unknown</c>.</param>
    public static void Start(SocketInteraction interaction, string interactionType, string commandName)
    {
        var activity = GridBotTelemetry.Source.StartActivity(GridBotTelemetry.Spans.Interaction);
        if (activity is not null)
        {
            activity.SetTag(GridBotTelemetry.Tags.CommandName, commandName);
            activity.SetTag(GridBotTelemetry.Tags.CommandType, "interaction");
            activity.SetTag(GridBotTelemetry.Tags.InteractionType, interactionType);
            activity.SetTag(GridBotTelemetry.Tags.DiscordUserId, interaction.User.Id);
            activity.SetTag(GridBotTelemetry.Tags.DiscordGuildId, interaction.GuildId);
            activity.SetTag(GridBotTelemetry.Tags.DiscordChannelId, interaction.ChannelId);
        }

        _entries.AddOrUpdate(
            interaction,
            new Entry(interactionType, commandName, GridBotMetrics.Interactions.InFlight.TrackInProgress(), activity)
        );
    }

    /// <summary>
    /// Completes tracking for an interaction. Does nothing if it is not tracked or already completed.
    /// </summary>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    /// <param name="outcome">A value from <see cref="MetricOutcomes"/>.</param>
    public static void Complete(SocketInteraction interaction, string outcome)
    {
        if (!_entries.TryGetValue(interaction, out var entry) || !_entries.Remove(interaction))
            return;

        entry.InFlight.Dispose();

        entry.Activity.SetOutcome(outcome);
        entry.Activity?.Dispose();

        GridBotMetrics.Interactions.Executions.WithLabels(entry.InteractionType, entry.CommandName, outcome).Inc();
        GridBotMetrics.Interactions.Duration
            .WithLabels(entry.InteractionType, entry.CommandName, outcome)
            .Observe(entry.Stopwatch.Elapsed.TotalSeconds);
    }
}
