namespace Grid.Bot.Eval.Runner.Models;

using Newtonsoft.Json;

/// <summary>
/// Represents the metadata returned by an evaluation script.
/// </summary>
public struct EvalMetadata
{
    /// <summary>
    /// Is the script a success?
    /// </summary>
    [JsonProperty("success")]
    public bool Success;

    /// <summary>
    /// The total execution time.
    /// </summary>
    [JsonProperty("executionTime")]
    public double ExecutionTime;

    /// <summary>
    /// The optional error message.
    /// </summary>
    [JsonProperty("errorMessage")]
    public string ErrorMessage;

    /// <summary>
    /// The standard output logs from the script execution.
    /// </summary>
    [JsonProperty("stdoutLogs")]
    public string StdoutLogs;

    /// <summary>
    /// The standard error logs from the script execution.
    /// </summary>
    [JsonProperty("stderrLogs")]
    public string StderrLogs;
}
