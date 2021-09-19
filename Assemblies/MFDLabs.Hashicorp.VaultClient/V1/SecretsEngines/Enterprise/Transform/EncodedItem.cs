using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.Transform
{
    /// <summary>
    /// Represents a single Encoded item.
    /// </summary>
    public class EncodedItem
    {
        /// <summary>
        /// Specifies the encoded value.
        /// </summary>
        [JsonProperty("encoded_value")]
        public string EncodedValue { get; set; }

        /// <summary>
        /// Specifies the base64 encoded tweak that was provided during encoding.
        /// </summary>
        [JsonProperty("tweak")]
        public string Tweak { get; set; }
    }
}