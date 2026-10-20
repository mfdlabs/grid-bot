namespace Grid.Bot.Events;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Discord.WebSocket;

using Prometheus;

using Grid.Bot.Metrics;

/// <summary>
/// Measures an interaction from dispatch to completion.
/// </summary>
/// <remarks>
/// Interactions run asynchronously, so the interaction service returns before the command has finished.
/// The duration is recorded when the interaction-executed event fires instead.
/// </remarks>
internal static class InteractionExecutionTracker
{
    private sealed class Entry(string interactionType, string commandName, IDisposable inFlight)
    {
        public readonly Stopwatch Stopwatch = Stopwatch.StartNew();
        public readonly string InteractionType = interactionType;
        public readonly string CommandName = commandName;
        public readonly IDisposable InFlight = inFlight;
    }

    private static readonly ConditionalWeakTable<SocketInteraction, Entry> _entries = [];

    /// <summary>
    /// Begins tracking an interaction.
    /// </summary>
    /// <param name="interaction">The <see cref="SocketInteraction"/>.</param>
    /// <param name="interactionType">The interaction type.</param>
    /// <param name="commandName">The command name, or <c>unknown</c>.</param>
    public static void Start(SocketInteraction interaction, string interactionType, string commandName)
        => _entries.AddOrUpdate(
            interaction,
            new Entry(interactionType, commandName, GridBotMetrics.Interactions.InFlight.TrackInProgress())
        );

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

        GridBotMetrics.Interactions.Executions.WithLabels(entry.InteractionType, entry.CommandName, outcome).Inc();
        GridBotMetrics.Interactions.Duration
            .WithLabels(entry.InteractionType, entry.CommandName, outcome)
            .Observe(entry.Stopwatch.Elapsed.TotalSeconds);
    }
}
