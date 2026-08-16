namespace Grid.Bot.Eval.Runner.Models;

using Newtonsoft.Json;

using Utility;

/// <summary>
/// Model for the result of an evaluation.
/// </summary>
public class ResultModel
{
    /// <summary>
    /// Gets or sets the result.
    /// </summary>
    [JsonProperty("result")]
    public string Result { get; set; }

    /// <summary>
    /// Gets or sets the eval metadata.
    /// </summary>
    [JsonProperty("metadata")]
    public EvalMetadata Metadata { get; set; }
}