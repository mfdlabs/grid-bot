﻿using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.PKI
{
    /// <summary>
    /// Represents the Certificate revocation response.
    /// </summary>
    public class RevokeCertificateResponse
    {
        /// <summary>
        /// Gets or sets the revocation time.
        /// </summary>
        /// <value>
        /// The revocation time.
        /// </value>
        [JsonProperty("revocation_time")]
        public int RevocationTime { get; set; }
    }
}