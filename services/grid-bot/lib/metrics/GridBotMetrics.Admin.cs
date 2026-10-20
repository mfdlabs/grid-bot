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
        /// Total number of users blacklisted.
        /// </summary>
        public static readonly Counter UsersBlacklisted = Prometheus.Metrics.CreateCounter(
            "admin_users_blacklisted_total",
            "Total number of users blacklisted."
        );
        /// <summary>
        /// Total number of users unblacklisted.
        /// </summary>
        public static readonly Counter UsersUnblacklisted = Prometheus.Metrics.CreateCounter(
            "admin_users_unblacklisted_total",
            "Total number of users unblacklisted."
        );
        /// <summary>
        /// Total number of users with privilaged access.
        /// </summary>
        public static readonly Counter UsersPrivilaged = Prometheus.Metrics.CreateCounter(
            "admin_users_privilaged_total",
            "Total number of users with privilaged access."
        );
        /// <summary>
        /// Total number of users demoted from admin.
        /// </summary>
        public static readonly Counter AdminUsersDemoted = Prometheus.Metrics.CreateCounter(
            "admin_users_demoted_total",
            "Total number of users demoted from admin."
        );
        /// <summary>
        /// Total number of users demoted from privilaged.
        /// </summary>
        public static readonly Counter PrivilegedUsersDemoted = Prometheus.Metrics.CreateCounter(
            "admin_users_demoted_privilaged_total",
            "Total number of users demoted from privilaged."
        );
        /// <summary>
        /// Total number of users promoted to admin.
        /// </summary>
        public static readonly Counter UsersPromoted = Prometheus.Metrics.CreateCounter(
            "admin_users_promoted_total",
            "Total number of users promoted to admin."
        );
    }
}
