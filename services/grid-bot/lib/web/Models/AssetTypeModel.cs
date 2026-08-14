namespace Grid.Bot.Web.Models;

using Newtonsoft.Json;

/// <summary>
/// A model container for the asset type.
/// </summary>
public class AssetTypeModel
{
    /// <summary>
    /// The Id
    /// </summary>
    [JsonProperty("id")]
    public int Id { get; set; }

    /// <summary>
    /// The name
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }
}