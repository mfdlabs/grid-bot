namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics that identify the running bot build.
    /// </summary>
    public static class Build
    {
        /// <summary>
        /// Grid.Bot build information
        /// </summary>
        public static readonly Gauge Information = Prometheus.Metrics.CreateGauge(
            "grid_bot_build_info",
            "Grid.Bot build information",
            "version",
            "git_hash",
            "git_branch",
            "build_timestamp"
        );
    }
}
