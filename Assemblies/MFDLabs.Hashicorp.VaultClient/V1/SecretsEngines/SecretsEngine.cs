﻿using Newtonsoft.Json;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;

namespace MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines
{
    /// <summary>
    /// Represents a secret backend.
    /// </summary>
    public class SecretsEngine : AbstractBackend
    {
        /// <summary>
        /// Gets or sets the type of the backend.
        /// </summary>
        /// <value>
        /// The type of the backend.
        /// </value>
        [JsonProperty("type")]
        public SecretsEngineType Type { get; set; }
    }
}