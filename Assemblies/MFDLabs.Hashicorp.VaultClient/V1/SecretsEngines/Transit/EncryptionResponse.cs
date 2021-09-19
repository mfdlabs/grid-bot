using System.Collections.Generic;
using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Transit
{
    /// <summary>
    /// Represents the encryption response.
    /// </summary>
    public class EncryptionResponse : CipherTextData
    {
        /// <summary>
        /// Gets or sets the batch results.
        /// </summary>
        /// <value>
        /// The batch results.
        /// </value>
        [JsonProperty("batch_results")]
        public List<CipherTextData> BatchedResults
        {
            get; set;
        }
    }
}