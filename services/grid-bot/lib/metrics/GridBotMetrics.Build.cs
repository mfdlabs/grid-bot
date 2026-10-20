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
        /// Identifies the running build by version, Git hash, branch, build timestamp, and the session ID unique to this run. The value is always 1.
        /// </summary>
        public static readonly Gauge Info = Prometheus.Metrics.CreateGauge(
            "gridbot_build_info",
            "Grid.Bot build information.",
            "version",
            "git_hash",
            "git_branch",
            "build_timestamp",
            "session_id"
        );
    }
}
