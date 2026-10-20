namespace Grid.Bot.Metrics;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// The name of the label added to every metric to identify the environment.
    /// </summary>
    public const string EnvironmentLabel = "environment";

    private static bool _environmentSet;

    /// <summary>
    /// Adds an <c>environment</c> label to every metric, including the runtime metrics.
    /// </summary>
    /// <remarks>
    /// The environment is supplied by the bot, so it does not depend on how the metrics are scraped.
    /// This must be called once at startup, before any metric is created. Metrics are created the first
    /// time their group is used, so call it before anything else. Calling it again is a no-op.
    /// </remarks>
    /// <param name="environment">The name of the environment, such as <c>staging</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="environment"/> cannot be null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">A metric has already been created.</exception>
    public static void SetEnvironment(string environment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(environment);

        if (_environmentSet)
            return;

        Prometheus.Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>
        {
            [EnvironmentLabel] = environment,
        });

        _environmentSet = true;
    }

    /// <summary>
    /// Creates every metric, and every series whose label values are a fixed set, with a value of zero.
    /// </summary>
    /// <remarks>
    /// A series that is only created by its first increment is first scraped with a value of 1. Prometheus
    /// has no earlier sample to compare it with, so <c>increase()</c> and <c>rate()</c> report 0 for that first
    /// event. Creating the series up front means the first event is counted. Call this after
    /// <see cref="SetEnvironment(string)"/>. Series whose labels are not known up front, such as a command name,
    /// are created where those values become known.
    /// </remarks>
    public static void InitializeSeries()
    {
        foreach (var group in typeof(GridBotMetrics).GetNestedTypes())
            RuntimeHelpers.RunClassConstructor(group.TypeHandle);

        foreach (var outcome in MetricOutcomes.All)
        {
            Scripts.Executions.WithLabels(outcome);
            Scripts.Duration.WithLabels(outcome);
            Renders.Executions.WithLabels(outcome);
            Renders.Duration.WithLabels(outcome);
        }

        foreach (var outcome in new[] { MetricOutcomes.Success, MetricOutcomes.Rejected, MetricOutcomes.Exception })
            GridJobs.AcquireDuration.WithLabels(outcome);

        foreach (var outcome in new[] { MetricOutcomes.Success, MetricOutcomes.Exception })
            ClientSettings.RefreshDuration.WithLabels(outcome);

        foreach (var recycle in new[] { "true", "false" })
            GridJobs.Closed.WithLabels(recycle);

        foreach (var source in new[] { MetricLabelValues.Source.Message, MetricLabelValues.Source.Interaction })
        {
            Requests.MaintenanceBypasses.WithLabels(source);

            foreach (var reason in new[] { MetricLabelValues.RejectionReason.Maintenance, MetricLabelValues.RejectionReason.Blacklisted })
                Requests.Rejected.WithLabels(source, reason);
        }

        foreach (var feature in new[] { MetricLabelValues.Feature.Render, MetricLabelValues.Feature.Script })
            foreach (var scope in new[] { MetricLabelValues.FloodScope.Global, MetricLabelValues.FloodScope.PerUser })
                Requests.FloodCheckRejections.WithLabels(feature, scope);

        foreach (var change in new[]
        {
            MetricLabelValues.RoleChange.Blacklisted, MetricLabelValues.RoleChange.Unblacklisted,
            MetricLabelValues.RoleChange.Privileged, MetricLabelValues.RoleChange.AdminDemoted,
            MetricLabelValues.RoleChange.PrivilegedDemoted, MetricLabelValues.RoleChange.Promoted,
        })
            Admin.RoleChanges.WithLabels(change);

        foreach (var method in new[] { MetricLabelValues.GrpcMethod.CheckHealth, MetricLabelValues.GrpcMethod.SetStatus })
            Grpc.Requests.WithLabels(method);

        foreach (var reason in new[] { MetricLabelValues.ScriptRejectedInput.NoContent, MetricLabelValues.ScriptRejectedInput.Unicode })
            Scripts.RejectedInputs.WithLabels(reason);

        foreach (var reason in new[] { MetricLabelValues.ScriptResultError.NonJsonSerializable, MetricLabelValues.ScriptResultError.NonAscii })
            Scripts.ResultErrors.WithLabels(reason);

        foreach (var context in new[]
        {
            MetricLabelValues.SyntaxErrorContext.GridServerMetadata, MetricLabelValues.SyntaxErrorContext.GridServerFault,
            MetricLabelValues.SyntaxErrorContext.PreParser,
        })
            Scripts.SyntaxErrors.WithLabels(context);

        foreach (var source in new[] { MetricLabelValues.LogSource.Command, MetricLabelValues.LogSource.Interaction })
            Scripts.Logged.WithLabels(source);

        foreach (var reason in new[] { MetricLabelValues.RenderInvalidInput.InvalidId, MetricLabelValues.RenderInvalidInput.BannedUser })
            Renders.InvalidInputs.WithLabels(reason);

        foreach (var backend in new[] { MetricLabelValues.RenderBackend.RbxThumbnails, MetricLabelValues.RenderBackend.GridServer })
            Renders.Backend.WithLabels(backend);
    }
}
