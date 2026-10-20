namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for administrative role changes.
    /// </summary>
    public static class Admin
    {
        /// <summary>
        /// Counts changes to a user's role, labeled by the kind of change (see <see cref="MetricLabelValues.RoleChange"/>).
        /// </summary>
        public static readonly Counter RoleChanges = Prometheus.Metrics.CreateCounter(
            "gridbot_admin_role_changes_total",
            "The total number of changes to a user's role by change.",
            "change"
        );
    }
}
