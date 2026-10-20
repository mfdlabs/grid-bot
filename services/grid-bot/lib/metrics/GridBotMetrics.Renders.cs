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
        /// Counts render requests, labeled by outcome.
        /// </summary>
        public static readonly Counter Executions = Prometheus.Metrics.CreateCounter(
            "gridbot_render_executions_total",
            "The total number of render requests by outcome.",
            "outcome"
        );

        /// <summary>
        /// Measures render request duration in seconds, labeled by outcome.
        /// </summary>
        public static readonly Histogram Duration = Prometheus.Metrics.CreateHistogram(
            "gridbot_render_duration_seconds",
            "The time it takes to handle a render request, labeled by outcome.",
            new HistogramConfiguration
            {
                LabelNames = ["outcome"],
                Buckets = MetricBuckets.Execution,
            }
        );

        /// <summary>
        /// Tracks the number of render requests currently being handled.
        /// </summary>
        public static readonly Gauge InFlight = Prometheus.Metrics.CreateGauge(
            "gridbot_renders_in_flight",
            "The number of render requests currently being handled."
        );

        /// <summary>
        /// Counts render requests made with a username rather than an ID.
        /// </summary>
        public static readonly Counter UsernameLookups = Prometheus.Metrics.CreateCounter(
            "gridbot_render_username_lookups_total",
            "The total number of render requests made with a username."
        );

        /// <summary>
        /// Counts render requests that were not accepted, labeled by reason (see <see cref="MetricLabelValues.RenderInvalidInput"/>).
        /// </summary>
        public static readonly Counter InvalidInputs = Prometheus.Metrics.CreateCounter(
            "gridbot_render_invalid_inputs_total",
            "The total number of render requests that were not accepted by reason.",
            "reason"
        );

        /// <summary>
        /// Counts thumbnails rendered, labeled by command type.
        /// </summary>
        public static readonly Counter ThumbnailsRendered = Prometheus.Metrics.CreateCounter(
            "gridbot_render_thumbnails_rendered_total",
            "The total number of thumbnails rendered by command type.",
            "command_type"
        );

        /// <summary>
        /// Counts renders, labeled by the backend that produced them (see <see cref="MetricLabelValues.RenderBackend"/>).
        /// </summary>
        public static readonly Counter Backend = Prometheus.Metrics.CreateCounter(
            "gridbot_render_backend_total",
            "The total number of renders by the backend that produced them.",
            "backend"
        );

        /// <summary>
        /// Counts statuses returned by rbx-thumbnails, labeled by status.
        /// </summary>
        public static readonly Counter RbxThumbnailsStatuses = Prometheus.Metrics.CreateCounter(
            "gridbot_render_rbx_thumbnails_statuses_total",
            "The total number of statuses returned by rbx-thumbnails by status.",
            "status"
        );

        /// <summary>
        /// Counts errors from rbx-thumbnails, labeled by the state it returned.
        /// </summary>
        public static readonly Counter RbxThumbnailsErrors = Prometheus.Metrics.CreateCounter(
            "gridbot_render_rbx_thumbnails_errors_total",
            "The total number of rbx-thumbnails errors by state.",
            "state"
        );

        /// <summary>
        /// Counts fetches from rbx-thumbnails, labeled by command type.
        /// </summary>
        public static readonly Counter RbxThumbnailsFetches = Prometheus.Metrics.CreateCounter(
            "gridbot_render_rbx_thumbnails_fetches_total",
            "The total number of fetches from rbx-thumbnails by command type.",
            "command_type"
        );

        /// <summary>
        /// Counts rbx-thumbnails fetches that used an identifier known not to render.
        /// </summary>
        public static readonly Counter RbxThumbnailsBlockedIdFetches = Prometheus.Metrics.CreateCounter(
            "gridbot_render_rbx_thumbnails_blocked_id_fetches_total",
            "The total number of rbx-thumbnails fetches that used an identifier known not to render."
        );

        /// <summary>
        /// Reports the percentage of users included in the rbx-thumbnails rollout.
        /// </summary>
        public static readonly Gauge RbxThumbnailsRolloutPercent = Prometheus.Metrics.CreateGauge(
            "gridbot_render_rbx_thumbnails_rollout_percent",
            "The percentage of users included in the rbx-thumbnails rollout."
        );

        /// <summary>
        /// Counts requests rejected by the Grid server renderer, labeled by rejection reason.
        /// </summary>
        public static readonly Counter LegacyRejections = Prometheus.Metrics.CreateCounter(
            "gridbot_render_legacy_rejections_total",
            "The total number of renders rejected by the Grid server renderer by reason.",
            "reason"
        );

        /// <summary>
        /// Counts errors from the Grid server renderer.
        /// </summary>
        public static readonly Counter LegacyErrors = Prometheus.Metrics.CreateCounter(
            "gridbot_render_legacy_errors_total",
            "The total number of errors from the Grid server renderer."
        );

        /// <summary>
        /// Reports the number of identifiers excluded from rendering.
        /// </summary>
        public static readonly Gauge BlockedIds = Prometheus.Metrics.CreateGauge(
            "gridbot_render_blocked_ids",
            "The number of identifiers excluded from rendering."
        );

        /// <summary>
        /// Reports the current local thumbnail cache size.
        /// </summary>
        public static readonly Gauge LocalCacheSize = Prometheus.Metrics.CreateGauge(
            "gridbot_render_local_cache_size",
            "The number of entries in the local thumbnail cache."
        );
    }
}
