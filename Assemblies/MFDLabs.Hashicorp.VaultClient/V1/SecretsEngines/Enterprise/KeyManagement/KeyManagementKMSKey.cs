using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.Enterprise.KeyManagement
{
    /// <summary>
    /// Key in KMS
    /// </summary>
    public class KeyManagementKMSKey
    {
        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("protection")]
        public string Protection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [JsonProperty("purpose")]
        public string Purpose { get; set; }
    }
}