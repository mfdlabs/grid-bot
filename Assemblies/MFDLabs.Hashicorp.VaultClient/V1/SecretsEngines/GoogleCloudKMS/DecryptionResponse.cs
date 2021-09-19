using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.GoogleCloudKMS
{
    /// <summary>
    /// Decryption output.
    /// </summary>
    public class DecryptionResponse
    {
        /// <summary>
        ///  Decrypted plain text.
        /// </summary>
        [JsonProperty("plaintext")]
        public string PlainText { get; set; }
    }
}