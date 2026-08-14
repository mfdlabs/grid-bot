namespace Grid.Bot.Web.Models.V2;

using Newtonsoft.Json;

/// <summary>
/// A model container for the avatar V2.
/// </summary>
/// <remarks>Skinny model with only required information</remarks>
public class AvatarModel
{
    /// <summary>
    /// The scales of the avatar.
    /// </summary>
    [JsonProperty("scales")]
    public ScaleModel Scales { get; set; }

    /// <summary>
    /// The player avatar type
    /// </summary>
    /// <remarks>This is an integer on the schema but is actually a string in the API response.</remarks>
    [JsonProperty("playerAvatarType")]
    public string PlayerAvatarType { get; set; }

    /// <summary>
    /// The body colors of the avatar.
    /// </summary>
    [JsonProperty("bodyColors")]
    public BodyColorsModel BodyColors { get; set; }

    /// <summary>
    /// The assets of the avatar.
    /// </summary>
    [JsonProperty("assets")]
    public AssetModel[] Assets { get; set; }
}