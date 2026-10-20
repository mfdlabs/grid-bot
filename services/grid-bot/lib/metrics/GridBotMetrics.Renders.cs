namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for avatar and thumbnail rendering.
    /// </summary>
    public static class Renders
    {
        /// <summary>
        /// The percentage of users that are using the rbx-thumbnails rollout.
        /// </summary>
        public static readonly Gauge RbxThumbnailsRolloutPercent = Prometheus.Metrics.CreateGauge(
            "avatar_thumbnails_rbx_thumbnails_rollout_percent",
            "The percentage of users that are using the rbx-thumbnails rollout."
        );
        /// <summary>
        /// The total number of IDs that are not to be used.
        /// </summary>
        public static readonly Gauge IdsNotToUse = Prometheus.Metrics.CreateGauge(
            "avatar_thumbnails_ids_not_to_use_total",
            "The total number of IDs that are not to be used."
        );
        /// <summary>
        /// The size of the local cache.
        /// </summary>
        public static readonly Gauge LocalCacheSize = Prometheus.Metrics.CreateGauge(
            "avatar_thumbnails_local_cache_size",
            "The size of the local cache."
        );
        /// <summary>
        /// The total number of statuses returned from rbx-thumbnails.
        /// </summary>
        public static readonly Counter RbxThumbnailsStatuses = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_rbx_thumbnails_status_total",
            "The total number of statuses returned from rbx-thumbnails.",
            "status"
        );
        /// <summary>
        /// The total number of fetches from rbx-thumbnails.
        /// </summary>
        public static readonly Counter RbxThumbnailsFetches = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_rbx_thumbnails_fetch_total",
            "The total number of fetches from rbx-thumbnails.",
            "user_id",
            "command_type"
        );
        /// <summary>
        /// The total number of times a blacklisted ID was used.
        /// </summary>
        public static readonly Counter RbxThumbnailsFetchesWithBlacklistedId = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_rbx_thumbnails_fetch_blacklisted_id_used_total",
            "The total number of times a blacklisted ID was used.",
            "user_id"
        );
        /// <summary>
        /// The total number of rendered thumbnails.
        /// </summary>
        public static readonly Counter ThumbnailsRendered = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_rendered_total",
            "The total number of rendered thumbnails.",
            "user_id",
            "command_type"
        );
        /// <summary>
        /// The total number of users that were rendered via rbx-thumbnails.
        /// </summary>
        public static readonly Counter ViaRbxThumbnails = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_via_rbx_thumbnails_total",
            "The total number of users that were rendered via rbx-thumbnails.",
            "user_id"
        );
        /// <summary>
        /// The total number of users that were rendered via the old method.
        /// </summary>
        public static readonly Counter ViaGridServer = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_via_grid_server_total",
            "The total number of users that were rendered via the old method.",
            "user_id",
            "place_id",
            "command_type",
            "dimension_x",
            "dimension_y"
        );
        /// <summary>
        /// The total number of legacy rejections.
        /// </summary>
        public static readonly Counter LegacyRejected = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_legacy_rejected_total",
            "The total number of legacy rejections.",
            "user_id",
            "rejection_reason"
        );
        /// <summary>
        /// The total number of legacy errors.
        /// </summary>
        public static readonly Counter LegacyErrors = Prometheus.Metrics.CreateCounter(
            "avatar_thumbnails_legacy_error_total",
            "The total number of legacy errors.",
            "user_id"
        );
        /// <summary>
        /// The total number of renders blocked by the global flood checker.
        /// </summary>
        public static readonly Counter BlockedByGlobalFloodChecker = Prometheus.Metrics.CreateCounter(
            "renders_blocked_by_global_flood_checker_total",
            "The total number of renders blocked by the global flood checker."
        );
        /// <summary>
        /// The total number of renders blocked by the per user flood checker.
        /// </summary>
        public static readonly Counter BlockedByPerUserFloodChecker = Prometheus.Metrics.CreateCounter(
            "renders_blocked_by_per_user_flood_checker_total",
            "The total number of renders blocked by the per user flood checker.",
            "user_id"
        );
        /// <summary>
        /// The total number of renders.
        /// </summary>
        public static readonly Counter Total = Prometheus.Metrics.CreateCounter(
            "renders_total",
            "The total number of renders.",
            "user_name_or_id"
        );
        /// <summary>
        /// The total number of renders via username.
        /// </summary>
        public static readonly Counter ViaUsername = Prometheus.Metrics.CreateCounter(
            "renders_via_username_total",
            "The total number of renders via username."
        );
        /// <summary>
        /// The total number of renders with invalid IDs.
        /// </summary>
        public static readonly Counter WithInvalidIds = Prometheus.Metrics.CreateCounter(
            "renders_with_invalid_ids_total",
            "The total number of renders with invalid IDs."
        );
        /// <summary>
        /// The total number of renders against banned users.
        /// </summary>
        public static readonly Counter AgainstBannedUsers = Prometheus.Metrics.CreateCounter(
            "renders_against_banned_users_total",
            "The total number of renders against banned users.",
            "user_name_or_id"
        );
        /// <summary>
        /// The total number of renders with errors.
        /// </summary>
        public static readonly Counter WithErrors = Prometheus.Metrics.CreateCounter(
            "renders_with_errors_total",
            "The total number of renders with errors."
        );
        /// <summary>
        /// The total number of renders with rbx-thumbnails errors.
        /// </summary>
        public static readonly Counter WithRbxThumbnailsErrors = Prometheus.Metrics.CreateCounter(
            "renders_with_rbx_thumbnails_errors_total",
            "The total number of renders with rbx-thumbnails errors.",
            "state"
        );
    }
}
