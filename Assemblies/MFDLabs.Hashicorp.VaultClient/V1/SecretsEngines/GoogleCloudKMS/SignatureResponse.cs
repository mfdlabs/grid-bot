using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.GoogleCloudKMS
{
    /// <summary>
    /// Signature output.
    /// </summary>
    public class SignatureResponse
    {
        /// <summary>
        /// The signature
        /// </summary>
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
}