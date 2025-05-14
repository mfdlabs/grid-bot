namespace Grid.Bot;

using Prometheus;

internal class ScriptExecutionPerformanceCounters
{
    public static readonly Counter TotalScriptExecutionsBlockedByGlobalFloodChecker = Metrics.CreateCounter(
        "script_executions_blocked_by_global_flood_checker_total",
        "The total number of script executions blocked by the global flood checker."
    );
    public static readonly Counter TotalScriptExecutionsBlockedByPerUserFloodChecker = Metrics.CreateCounter(
        "script_executions_blocked_by_per_user_flood_checker_total",
        "The total number of script executions blocked by the per user flood checker.",
        "user_id"
    );
    public static readonly Counter TotalScriptExecutionsWithSyntaxErrors = Metrics.CreateCounter(
        "script_executions_with_syntax_errors_total",
        "The total number of script executions with syntax errors.",
        "context"
    );
    public static readonly Counter TotalScriptExecutionsFromFiles = Metrics.CreateCounter(
        "script_executions_from_files_total",
        "The total number of script executions from files.",
        "file_name",
        "file_size"
    );
    public static readonly Counter TotalScriptExecutionsWithNoContent = Metrics.CreateCounter(
        "script_executions_with_no_content_total",
        "The total number of script executions with no content."
    );
    public static readonly Counter TotalScriptExecutionsWithUnicode = Metrics.CreateCounter(
        "script_executions_with_unicode_total",
        "The total number of script executions with unicode."
    );
    public static readonly Counter TotalScriptExecutionsByUser = Metrics.CreateCounter(
        "script_executions_by_user_total",
        "The total number of script executions by user.",
        "user_id"
    );
    public static readonly Counter TotalScriptExecutionsWithResultsExceedingMaxSize = Metrics.CreateCounter(
        "script_executions_with_results_exceeding_max_size_total",
        "The total number of script executions with result exceeding max size.",
        "total_size"
    );
    public static readonly Counter TotalScriptExecutionsWithResultsViaFiles = Metrics.CreateCounter(
        "script_executions_with_results_via_file_total",
        "The total number of script executions with results via file."
    );
    public static readonly Counter TotalSuccessfulScriptExecutions = Metrics.CreateCounter(
        "script_executions_success_total",
        "The total number of successful script executions."
    );
    public static readonly Counter TotalFailedScriptExecutionsDueToLuaError = Metrics.CreateCounter(
        "script_executions_failed_due_to_lua_error_total",
        "The total number of script executions failed due to Lua error."
    );
    public static readonly Histogram ScriptExecutionAverageExecutionTime = Metrics.CreateHistogram(
        "script_execution_average_execution_time_seconds",
        "The average execution time of script executions in seconds.",
        new HistogramConfiguration
        {
            Buckets = Histogram.LinearBuckets(0, 0.1, 100)
        }
    );
    public static readonly Counter TotalScriptExecutionsUsingLuaVM = Metrics.CreateCounter(
        "script_executions_using_lua_vm_total",
        "The total number of script executions using Lua VM."
    );
    public static readonly Counter TotalScriptExecutionsWithNonJsonSerializableResults = Metrics.CreateCounter(
        "script_executions_with_non_json_serializable_results_total",
        "The total number of script executions with non JSON serializable results."
    );
    public static readonly Counter TotalScriptExecutionsWithNonAsciiResults = Metrics.CreateCounter(
        "script_executions_with_non_ascii_results_total",
        "The total number of script executions with non ASCII results."
    );
    public static readonly Counter TotalScriptExecutionsThatTimedOut = Metrics.CreateCounter(
        "script_executions_timed_out_total",
        "The total number of script executions that timed out."
    );
    public static readonly Counter TotalScriptExecutionsWithUnexpectedExceptions = Metrics.CreateCounter(
        "script_executions_with_unexpected_exceptions_total",
        "The total number of script executions with unexpected exceptions.",
        "exception_type"
    );
}
