namespace Grid.Bot.Metrics;

using System;
using System.Collections.Generic;

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
}
