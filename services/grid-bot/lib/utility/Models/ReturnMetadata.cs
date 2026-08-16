namespace Grid.Bot.Utility;

using Newtonsoft.Json;

/// <summary>
/// Represents the metadata returned by the lua-vm script.
/// </summary>
public struct ReturnMetadata
{
    /// <summary>
    /// Is the script a success?
    /// </summary>
    [JsonProperty("success")]
    public bool Success;

    /// <summary>
    /// The total execution time.
    /// </summary>
    [JsonProperty("execution_time")]
    public double ExecutionTime;

    /// <summary>
    /// The optional error message.
    /// </summary>
    [JsonProperty("error_message")]
    public string ErrorMessage;

    /// <summary>
    /// The optional logs.
    /// </summary>
    [JsonProperty("logs")]
    public string Logs;
}
