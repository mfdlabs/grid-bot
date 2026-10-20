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
        public static readonly Counter Executions = Prometheus.Metrics.CreateCounter(
            "gridbot_script_executions_total",
            "The total number of script executions by outcome.",
            "outcome"
        );

        /// <summary>
        /// Measures how long a script execution request takes to handle in seconds, labeled by outcome.
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
        /// Measures how long scripts ran for in seconds, as reported by the script runtime.
        /// </summary>
        public static readonly Histogram RunDuration = Prometheus.Metrics.CreateHistogram(
            "gridbot_script_run_duration_seconds",
            "The time scripts ran for, as reported by the script runtime.",
            new HistogramConfiguration
            {
                Buckets = MetricBuckets.Execution,
            }
        );

        /// <summary>
        /// Counts script syntax errors, labeled by where the error was found.
        /// </summary>
        public static readonly Counter SyntaxErrors = Prometheus.Metrics.CreateCounter(
            "gridbot_script_syntax_errors_total",
            "The total number of script syntax errors by context.",
            "context"
        );

        /// <summary>
        /// Counts scripts that were not accepted, labeled by reason (see <see cref="MetricLabelValues.ScriptRejectedInput"/>).
        /// </summary>
        public static readonly Counter RejectedInputs = Prometheus.Metrics.CreateCounter(
            "gridbot_script_rejected_inputs_total",
            "The total number of scripts that were not accepted by reason.",
            "reason"
        );

        /// <summary>
        /// Counts script executions that read the script from a file.
        /// </summary>
        public static readonly Counter FromFiles = Prometheus.Metrics.CreateCounter(
            "gridbot_script_executions_from_files_total",
            "The total number of script executions that read the script from a file."
        );

        /// <summary>
        /// Counts script executions that use the Lua virtual machine.
        /// </summary>
        public static readonly Counter UsingLuaVm = Prometheus.Metrics.CreateCounter(
            "gridbot_script_lua_vm_executions_total",
            "The total number of script executions that use the Lua virtual machine."
        );

        /// <summary>
        /// Counts script results that were unusable, labeled by reason (see <see cref="MetricLabelValues.ScriptResultError"/>).
        /// </summary>
        public static readonly Counter ResultErrors = Prometheus.Metrics.CreateCounter(
            "gridbot_script_result_errors_total",
            "The total number of unusable script results by reason.",
            "reason"
        );

        /// <summary>
        /// Counts script results that exceeded the maximum size.
        /// </summary>
        public static readonly Counter OversizedResults = Prometheus.Metrics.CreateCounter(
            "gridbot_script_oversized_results_total",
            "The total number of script results that exceeded the maximum size."
        );

        /// <summary>
        /// Counts script results delivered as a file.
        /// </summary>
        public static readonly Counter ResultsViaFile = Prometheus.Metrics.CreateCounter(
            "gridbot_script_results_via_file_total",
            "The total number of script results delivered as a file."
        );

        /// <summary>
        /// Counts script executions that failed with an unexpected exception, labeled by exception type.
        /// </summary>
        public static readonly Counter UnexpectedExceptions = Prometheus.Metrics.CreateCounter(
            "gridbot_script_unexpected_exceptions_total",
            "The total number of script executions that failed with an unexpected exception by type.",
            "exception_type"
        );

        /// <summary>
        /// Reports the number of distinct script hashes logged.
        /// </summary>
        public static readonly Gauge LoggedHashes = Prometheus.Metrics.CreateGauge(
            "gridbot_script_logged_hashes",
            "The number of distinct script hashes logged."
        );

        /// <summary>
        /// Counts scripts logged, labeled by whether they came from a command or an interaction.
        /// </summary>
        public static readonly Counter Logged = Prometheus.Metrics.CreateCounter(
            "gridbot_script_logged_total",
            "The total number of scripts logged by source.",
            "source"
        );

        /// <summary>
        /// Counts scripts that were already logged.
        /// </summary>
        public static readonly Counter LoggedDuplicates = Prometheus.Metrics.CreateCounter(
            "gridbot_script_logged_duplicates_total",
            "The total number of scripts that were already logged."
        );
    }
}
