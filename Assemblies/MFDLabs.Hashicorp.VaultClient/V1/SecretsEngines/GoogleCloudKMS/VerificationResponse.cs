using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.GoogleCloudKMS
{
    /// <summary>
    /// Verification output.
    /// </summary>
    public class VerificationResponse
    {
        /// <summary>
        /// Flag to indicate if signature is valid.
        /// </summary>
        [JsonProperty("valid")]
        public bool Valid { get; set; }
    }
}