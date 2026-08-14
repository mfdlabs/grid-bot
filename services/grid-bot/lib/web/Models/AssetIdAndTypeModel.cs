namespace Grid.Bot.Web.Models;

using Newtonsoft.Json;

/// <summary>
/// A model container for the asset ID and type of an avatar.
/// </summary>
public class AssetIdAndTypeModel
{
    /// <summary>
    /// The asset ID of the avatar.
    /// </summary>
    [JsonProperty("assetId")]
    public long AssetId { get; set; }

    /// <summary>
    /// The asset type ID of the avatar.
    /// </summary>
    [JsonProperty("assetTypeId")]
    public long AssetTypeId { get; set; }
}