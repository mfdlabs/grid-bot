namespace Grid.Bot;

using Prometheus;
using Grid.Bot.Metrics;

internal class ScriptExecutionPerformanceCounters
{
    public static readonly Counter TotalScriptExecutionsBlockedByGlobalFloodChecker = GridBotMetrics.Scripts.BlockedByGlobalFloodChecker;
    public static readonly Counter TotalScriptExecutionsBlockedByPerUserFloodChecker = GridBotMetrics.Scripts.BlockedByPerUserFloodChecker;
    public static readonly Counter TotalScriptExecutionsWithSyntaxErrors = GridBotMetrics.Scripts.WithSyntaxErrors;
    public static readonly Counter TotalScriptExecutionsFromFiles = GridBotMetrics.Scripts.FromFiles;
    public static readonly Counter TotalScriptExecutionsWithNoContent = GridBotMetrics.Scripts.WithNoContent;
    public static readonly Counter TotalScriptExecutionsWithUnicode = GridBotMetrics.Scripts.WithUnicode;
    public static readonly Counter TotalScriptExecutionsByUser = GridBotMetrics.Scripts.ByUser;
    public static readonly Counter TotalScriptExecutionsWithResultsExceedingMaxSize = GridBotMetrics.Scripts.WithResultsExceedingMaxSize;
    public static readonly Counter TotalScriptExecutionsWithResultsViaFiles = GridBotMetrics.Scripts.WithResultsViaFiles;
    public static readonly Counter TotalSuccessfulScriptExecutions = GridBotMetrics.Scripts.Successful;
    public static readonly Counter TotalFailedScriptExecutionsDueToLuaError = GridBotMetrics.Scripts.FailedDueToLuaError;
    public static readonly Histogram ScriptExecutionAverageExecutionTime = GridBotMetrics.Scripts.ExecutionTime;
    public static readonly Counter TotalScriptExecutionsUsingLuaVM = GridBotMetrics.Scripts.UsingLuaVm;
    public static readonly Counter TotalScriptExecutionsWithNonJsonSerializableResults = GridBotMetrics.Scripts.WithNonJsonSerializableResults;
    public static readonly Counter TotalScriptExecutionsWithNonAsciiResults = GridBotMetrics.Scripts.WithNonAsciiResults;
    public static readonly Counter TotalScriptExecutionsThatTimedOut = GridBotMetrics.Scripts.TimedOut;
    public static readonly Counter TotalScriptExecutionsWithUnexpectedExceptions = GridBotMetrics.Scripts.WithUnexpectedExceptions;
}
