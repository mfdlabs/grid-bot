namespace Grid.Bot.Metrics;

using Prometheus;

/// <summary>
/// Defines the Prometheus metrics used by the Grid.Bot service.
/// </summary>
public static partial class GridBotMetrics
{
    /// <summary>
    /// Metrics for executing and logging user scripts.
    /// </summary>
    public static class Scripts
    {
        /// <summary>
        /// Counts script executions, labeled by outcome.
        /// </summary>
        /// <remarks>
        /// Replaces <see cref="ByUser"/>, which has an unbounded user label.
        /// </remarks>
        public static readonly Counter Executions = Prometheus.Metrics.CreateCounter(
            "gridbot_script_executions_total",
            "The total number of script executions by outcome.",
            "outcome"
        );

        /// <summary>
        /// Measures script execution duration in seconds, labeled by outcome.
        /// </summary>
        public static readonly Histogram Duration = Prometheus.Metrics.CreateHistogram(
            "gridbot_script_execution_duration_seconds",
            "The time it takes to handle a script execution request, labeled by outcome.",
            new HistogramConfiguration
            {
                LabelNames = ["outcome"],
                Buckets = MetricBuckets.Execution,
            }
        );

        /// <summary>
        /// Tracks the number of script executions currently being handled.
        /// </summary>
        public static readonly Gauge InFlight = Prometheus.Metrics.CreateGauge(
            "gridbot_script_executions_in_flight",
            "The number of script executions currently being handled."
        );

        /// <summary>
        /// Total number of script hashes logged ever
        /// </summary>
        public static readonly Gauge LoggedScriptHashes = Prometheus.Metrics.CreateGauge(
            "script_logging_script_hashes_total",
            "Total number of script hashes logged ever"
        );
        /// <summary>
        /// Total number of scripts logged
        /// </summary>
        public static readonly Counter Logged = Prometheus.Metrics.CreateCounter(
            "script_logging_scripts_logged_total",
            "Total number of scripts logged",
            "source"
        );
        /// <summary>
        /// Total number of existing scripts logged
        /// </summary>
        public static readonly Counter ExistingScriptsLogged = Prometheus.Metrics.CreateCounter(
            "script_logging_existing_scripts_logged_total",
            "Total number of existing scripts logged",
            "script_hash"
        );
        /// <summary>
        /// The total number of script executions blocked by the global flood checker.
        /// </summary>
        public static readonly Counter BlockedByGlobalFloodChecker = Prometheus.Metrics.CreateCounter(
            "script_executions_blocked_by_global_flood_checker_total",
            "The total number of script executions blocked by the global flood checker."
        );
        /// <summary>
        /// The total number of script executions blocked by the per user flood checker.
        /// </summary>
        public static readonly Counter BlockedByPerUserFloodChecker = Prometheus.Metrics.CreateCounter(
            "script_executions_blocked_by_per_user_flood_checker_total",
            "The total number of script executions blocked by the per user flood checker.",
            "user_id"
        );
        /// <summary>
        /// The total number of script executions with syntax errors.
        /// </summary>
        public static readonly Counter WithSyntaxErrors = Prometheus.Metrics.CreateCounter(
            "script_executions_with_syntax_errors_total",
            "The total number of script executions with syntax errors.",
            "context"
        );
        /// <summary>
        /// The total number of script executions from files.
        /// </summary>
        public static readonly Counter FromFiles = Prometheus.Metrics.CreateCounter(
            "script_executions_from_files_total",
            "The total number of script executions from files.",
            "file_name",
            "file_size"
        );
        /// <summary>
        /// The total number of script executions with no content.
        /// </summary>
        public static readonly Counter WithNoContent = Prometheus.Metrics.CreateCounter(
            "script_executions_with_no_content_total",
            "The total number of script executions with no content."
        );
        /// <summary>
        /// The total number of script executions with unicode.
        /// </summary>
        public static readonly Counter WithUnicode = Prometheus.Metrics.CreateCounter(
            "script_executions_with_unicode_total",
            "The total number of script executions with unicode."
        );
        /// <summary>
        /// The total number of script executions by user.
        /// </summary>
        public static readonly Counter ByUser = Prometheus.Metrics.CreateCounter(
            "script_executions_by_user_total",
            "The total number of script executions by user.",
            "user_id"
        );
        /// <summary>
        /// The total number of script executions with result exceeding max size.
        /// </summary>
        public static readonly Counter WithResultsExceedingMaxSize = Prometheus.Metrics.CreateCounter(
            "script_executions_with_results_exceeding_max_size_total",
            "The total number of script executions with result exceeding max size.",
            "total_size"
        );
        /// <summary>
        /// The total number of script executions with results via file.
        /// </summary>
        public static readonly Counter WithResultsViaFiles = Prometheus.Metrics.CreateCounter(
            "script_executions_with_results_via_file_total",
            "The total number of script executions with results via file."
        );
        /// <summary>
        /// The total number of successful script executions.
        /// </summary>
        public static readonly Counter Successful = Prometheus.Metrics.CreateCounter(
            "script_executions_success_total",
            "The total number of successful script executions."
        );
        /// <summary>
        /// The total number of script executions failed due to Lua error.
        /// </summary>
        public static readonly Counter FailedDueToLuaError = Prometheus.Metrics.CreateCounter(
            "script_executions_failed_due_to_lua_error_total",
            "The total number of script executions failed due to Lua error."
        );
        /// <summary>
        /// The average execution time of script executions in seconds.
        /// </summary>
        public static readonly Histogram ExecutionTime = Prometheus.Metrics.CreateHistogram(
            "script_execution_average_execution_time_seconds",
            "The average execution time of script executions in seconds."
        );
        /// <summary>
        /// The total number of script executions using Lua VM.
        /// </summary>
        public static readonly Counter UsingLuaVm = Prometheus.Metrics.CreateCounter(
            "script_executions_using_lua_vm_total",
            "The total number of script executions using Lua VM."
        );
        /// <summary>
        /// The total number of script executions with non JSON serializable results.
        /// </summary>
        public static readonly Counter WithNonJsonSerializableResults = Prometheus.Metrics.CreateCounter(
            "script_executions_with_non_json_serializable_results_total",
            "The total number of script executions with non JSON serializable results."
        );
        /// <summary>
        /// The total number of script executions with non ASCII results.
        /// </summary>
        public static readonly Counter WithNonAsciiResults = Prometheus.Metrics.CreateCounter(
            "script_executions_with_non_ascii_results_total",
            "The total number of script executions with non ASCII results."
        );
        /// <summary>
        /// The total number of script executions that timed out.
        /// </summary>
        public static readonly Counter TimedOut = Prometheus.Metrics.CreateCounter(
            "script_executions_timed_out_total",
            "The total number of script executions that timed out."
        );
        /// <summary>
        /// The total number of script executions with unexpected exceptions.
        /// </summary>
        public static readonly Counter WithUnexpectedExceptions = Prometheus.Metrics.CreateCounter(
            "script_executions_with_unexpected_exceptions_total",
            "The total number of script executions with unexpected exceptions.",
            "exception_type"
        );
    }
}
