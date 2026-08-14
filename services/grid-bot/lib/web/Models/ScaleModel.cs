namespace Grid.Bot.Web.Models;

using Newtonsoft.Json;

/// <summary>
/// A model container for the scale of an avatar.
/// </summary>
public class ScaleModel
{
    /// <summary>
    /// The height of the avatar.
    /// </summary>
    [JsonProperty("height")]
    public double Height { get; set; }

    /// <summary>
    /// The width of the avatar.
    /// </summary>
    [JsonProperty("width")]
    public double Width { get; set; }

    /// <summary>
    /// The head size of the avatar.
    /// </summary>
    [JsonProperty("head")]
    public double Head { get; set; }

    /// <summary>
    /// The depth of the avatar.
    /// </summary>
    [JsonProperty("depth")]
    public double Depth { get; set; }

    /// <summary>
    /// The proportion of the avatar.
    /// </summary>
    [JsonProperty("proportion")]
    public double Proportion { get; set; }

    /// <summary>
    /// The body type of the avatar.
    /// </summary>
    [JsonProperty("bodyType")]
    public double BodyType { get; set; }
}