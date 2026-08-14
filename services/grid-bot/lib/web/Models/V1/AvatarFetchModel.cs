namespace Grid.Bot.Web.Models.V1;

using System.Collections.Generic;

using Newtonsoft.Json;

/// <summary>
/// A model container for the avatar fetch request.
/// </summary>
public class AvatarFetchModel
{
    /// <summary>
    /// The resolved avatar type.
    /// </summary>
    [JsonProperty("resolvedAvatarType")]
    public string ResolvedAvatarType { get; set; }

    /// <summary>
    /// The equipped gear version IDs.
    /// </summary>
    [JsonProperty("equippedGearVersionIds")]
    public long[] EquippedGearVersionIds { get; set; }

    /// <summary>
    /// The backpack gear version IDs.
    /// </summary>
    [JsonProperty("backpackGearVersionIds")]
    public long[] BackpackGearVersionIds { get; set; }

    /// <summary>
    /// The asset and asset type IDs.
    /// </summary>
    [JsonProperty("assetAndAssetTypeIds")]
    public AssetIdAndTypeModel[] AssetAndAssetTypeIds { get; set; }

    /// <summary>
    /// The animation asset IDs.
    /// </summary>
    [JsonProperty("animationAssetIds")]
    public Dictionary<string, long> AnimationAssetIds { get; set; }

    /// <summary>
    /// The body colors of the avatar.
    /// </summary>
    [JsonProperty("bodyColors")]
    public BodyColorsModel BodyColors { get; set; }

    /// <summary>
    /// The scales of the avatar.
    /// </summary>
    [JsonProperty("scales")]
    public ScaleModel Scales { get; set; }
}