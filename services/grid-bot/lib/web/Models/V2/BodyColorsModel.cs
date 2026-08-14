namespace Grid.Bot.Web.Models.V2;

using Newtonsoft.Json;

/// <summary>
/// A model container brick colour IDs for each body part.
/// </summary>
public class BodyColorsModel
{
    /// <summary>
    /// Gets or sets the head brick colour ID.
    /// </summary>
    [JsonProperty("headColorId")]
    public int HeadColorId { get; set; }

    /// <summary>
    /// Gets or sets the torso brick colour ID.
    /// </summary>
    [JsonProperty("torsoColorId")]
    public int TorsoColorId { get; set; }

    /// <summary>
    /// Gets or sets the right arm brick colour ID.
    /// </summary>
    [JsonProperty("rightArmColorId")]
    public int RightArmColorId { get; set; }

    /// <summary>
    /// Gets or sets the left arm brick colour ID.
    /// </summary>
    [JsonProperty("leftArmColorId")]
    public int LeftArmColorId { get; set; }

    /// <summary>
    /// Gets or sets the right leg brick colour ID.
    /// </summary>
    [JsonProperty("rightLegColorId")]
    public int RightLegColorId { get; set; }

    /// <summary>
    /// Gets or sets the left leg brick colour ID.
    /// </summary>
    [JsonProperty("leftLegColorId")]
    public int LeftLegColorId { get; set; }
}