namespace Grid.Bot.Web.Models;

using Newtonsoft.Json;

/// <summary>
/// A model container for the asset.
/// </summary>
/// <remarks>Skinny model with only required information</remarks>
public class AssetModel
{
    /// <summary>
    /// The Id
    /// </summary>
    [JsonProperty("id")]
    public long Id { get; set; }

    /// <summary>
    /// The asset type
    /// </summary>
    [JsonProperty("assetType")]
    public AssetTypeModel AssetType { get; set; }

    /// <summary>
    /// The current version Id
    /// </summary>
    [JsonProperty("currentVersionId")]
    public long CurrentVersionId { get; set; }
}