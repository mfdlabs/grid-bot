namespace Grid.Bot.Web.Models.V1;

using Newtonsoft.Json;

using BodyColorsModelV2 = Grid.Bot.Web.Models.V2.BodyColorsModel;

/// <summary>
/// A model container BrickColor ids for each body part.
/// </summary>
public class BodyColorsModel
{
    /// <summary>
    /// Gets or sets the head color ID.
    /// </summary>
    [JsonProperty(nameof(HeadColor))]
    public int HeadColor { get; set; }

    /// <summary>
    /// Gets or sets the torso color ID.
    /// </summary>
    [JsonProperty(nameof(TorsoColor))]
    public int TorsoColor { get; set; }

    /// <summary>
    /// Gets or sets the right arm color ID.
    /// </summary>
    [JsonProperty(nameof(RightArmColor))]
    public int RightArmColor { get; set; }

    /// <summary>
    /// Gets or sets the left arm color ID.
    /// </summary>
    [JsonProperty(nameof(LeftArmColor))]
    public int LeftArmColor { get; set; }

    /// <summary>
    /// Gets or sets the right leg color ID.
    /// </summary>
    [JsonProperty(nameof(RightLegColor))]
    public int RightLegColor { get; set; }

    /// <summary>
    /// Gets or sets the left leg color ID.
    /// </summary>
    [JsonProperty(nameof(LeftLegColor))]
    public int LeftLegColor { get; set; }

    /// <summary>
    /// Converts a V2 model to a V1 model.
    /// </summary>
    /// <param name="model">The V2 model.</param>
    /// <returns>A V1 model.</returns>
    public static BodyColorsModel FromV2(BodyColorsModelV2 model)
        => new()
        {
            HeadColor = model.HeadColorId,
            TorsoColor = model.TorsoColorId,
            RightArmColor = model.RightArmColorId,
            LeftArmColor = model.LeftArmColorId,
            RightLegColor = model.RightLegColorId,
            LeftLegColor = model.LeftLegColorId
        };
}