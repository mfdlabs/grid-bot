namespace Grid.Bot.Telemetry;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Grid.Bot.Metrics;

/// <summary>
/// Defines the tracing source, span names, and attribute names used by the Grid.Bot service.
/// </summary>
/// <remarks>
/// Spans are only created while a tracer is listening to <see cref="Source"/>, so starting one while
/// tracing is disabled returns <see langword="null"/> and is effectively free.
/// </remarks>
public static class GridBotTelemetry
{
    /// <summary>
    /// The name of the <see cref="ActivitySource"/>. Tracers subscribe to this name.
    /// </summary>
    public const string SourceName = "Grid.Bot";

    /// <summary>
    /// The <see cref="ActivitySource"/> that all Grid.Bot spans are started from.
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName);

    /// <summary>
    /// The names of the spans.
    /// </summary>
    public static class Spans
    {
        /// <summary>
        /// A text command, from dispatch to completion.
        /// </summary>
        public const string Command = "command.execute";

        /// <summary>
        /// An interaction, from dispatch to completion.
        /// </summary>
        public const string Interaction = "interaction.execute";

        /// <summary>
        /// A script execution request.
        /// </summary>
        public const string Script = "script.execute";

        /// <summary>
        /// Logging a script.
        /// </summary>
        public const string ScriptLog = "script.log";

        /// <summary>
        /// A render request.
        /// </summary>
        public const string Render = "render.execute";

        /// <summary>
        /// Acquiring a Grid job, including waiting for a ready instance.
        /// </summary>
        public const string GridJobAcquire = "grid.job.acquire";

        /// <summary>
        /// Executing a job on the Grid server over SOAP.
        /// </summary>
        public const string GridSoapExecute = "grid.soap.execute";

        /// <summary>
        /// Sending a response to Discord.
        /// </summary>
        public const string DiscordRespond = "discord.respond";

        /// <summary>
        /// Refreshing the client settings.
        /// </summary>
        public const string ClientSettingsRefresh = "client_settings.refresh";
    }

    /// <summary>
    /// The names of the span attributes.
    /// </summary>
    /// <remarks>
    /// Unlike metric labels, span attributes may carry request-specific values such as identifiers.
    /// Never record message content or scripts.
    /// </remarks>
    public static class Tags
    {
        /// <summary>
        /// The outcome, a value from <see cref="MetricOutcomes"/>.
        /// </summary>
        public const string Outcome = "gridbot.outcome";

        /// <summary>
        /// The name of the command.
        /// </summary>
        public const string CommandName = "gridbot.command.name";

        /// <summary>
        /// The kind of command, <c>text</c> or <c>interaction</c>.
        /// </summary>
        public const string CommandType = "gridbot.command.type";

        /// <summary>
        /// The Discord interaction type.
        /// </summary>
        public const string InteractionType = "gridbot.interaction.type";

        /// <summary>
        /// The Discord user identifier.
        /// </summary>
        public const string DiscordUserId = "discord.user.id";

        /// <summary>
        /// The Discord guild identifier.
        /// </summary>
        public const string DiscordGuildId = "discord.guild.id";

        /// <summary>
        /// The Discord channel identifier.
        /// </summary>
        public const string DiscordChannelId = "discord.channel.id";

        /// <summary>
        /// The identifier of the Grid job.
        /// </summary>
        public const string GridJobId = "gridbot.grid.job.id";

        /// <summary>
        /// The reason a Grid job was rejected.
        /// </summary>
        public const string GridJobRejectionReason = "gridbot.grid.job.rejection_reason";

        /// <summary>
        /// Whether a response was sent to an interaction or a message.
        /// </summary>
        public const string RespondTarget = "gridbot.discord.respond.target";
    }

    /// <summary>
    /// Records an outcome on a span. Unexpected failures and timeouts mark the span as an error.
    /// </summary>
    /// <param name="activity">The span, which may be <see langword="null"/> when tracing is disabled.</param>
    /// <param name="outcome">A value from <see cref="MetricOutcomes"/>.</param>
    /// <returns>The span.</returns>
    public static Activity SetOutcome(this Activity activity, string outcome)
    {
        if (activity is null)
            return null;

        activity.SetTag(Tags.Outcome, outcome);

        if (outcome is MetricOutcomes.Exception or MetricOutcomes.Timeout)
            activity.SetStatus(ActivityStatusCode.Error, outcome);

        return activity;
    }

    /// <summary>
    /// Runs an operation inside a span. A thrown exception marks the span as an error and is rethrown.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="spanName">The name of the span, from <see cref="Spans"/>.</param>
    /// <param name="operation">The operation to run.</param>
    /// <param name="kind">The <see cref="ActivityKind"/> of the span.</param>
    /// <returns>The result of <paramref name="operation"/>.</returns>
    public static T Run<T>(string spanName, Func<T> operation, ActivityKind kind = ActivityKind.Internal)
    {
        using var activity = Source.StartActivity(spanName, kind);

        try
        {
            return operation();
        }
        catch (Exception ex)
        {
            activity.SetError(ex);

            throw;
        }
    }

    /// <summary>
    /// Runs an asynchronous operation inside a span. A thrown exception marks the span as an error and is rethrown.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="spanName">The name of the span, from <see cref="Spans"/>.</param>
    /// <param name="operation">The operation to run.</param>
    /// <param name="kind">The <see cref="ActivityKind"/> of the span.</param>
    /// <returns>The result of <paramref name="operation"/>.</returns>
    public static async Task<T> RunAsync<T>(string spanName, Func<Task<T>> operation, ActivityKind kind = ActivityKind.Internal)
    {
        using var activity = Source.StartActivity(spanName, kind);

        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity.SetError(ex);

            throw;
        }
    }

    /// <summary>
    /// Runs an asynchronous operation inside a span. A thrown exception marks the span as an error and is rethrown.
    /// </summary>
    /// <param name="spanName">The name of the span, from <see cref="Spans"/>.</param>
    /// <param name="operation">The operation to run.</param>
    /// <param name="kind">The <see cref="ActivityKind"/> of the span.</param>
    /// <param name="configure">An optional callback to tag the span. Only called when the span is being recorded.</param>
    /// <returns>A <see cref="Task"/> that completes when <paramref name="operation"/> does.</returns>
    public static async Task RunAsync(
        string spanName,
        Func<Task> operation,
        ActivityKind kind = ActivityKind.Internal,
        Action<Activity> configure = null
    )
    {
        using var activity = Source.StartActivity(spanName, kind);

        if (activity is not null)
            configure?.Invoke(activity);

        try
        {
            await operation().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            activity.SetError(ex);

            throw;
        }
    }

    // Only the exception type is recorded, messages can contain user input.
    private static void SetError(this Activity activity, Exception exception)
    {
        if (activity is null)
            return;

        activity.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
        activity.SetTag("exception.type", exception.GetType().FullName);
    }

    /// <summary>
    /// Gets a log prefix identifying the current trace, or an empty string when there is none.
    /// </summary>
    /// <returns>A value like <c>trace_id=4bf92f3577b34da6a3ce929d0e0e4736</c>.</returns>
    public static string GetTraceLogPrefix()
        => Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity
            ? $"trace_id={activity.TraceId}"
            : string.Empty;
}
