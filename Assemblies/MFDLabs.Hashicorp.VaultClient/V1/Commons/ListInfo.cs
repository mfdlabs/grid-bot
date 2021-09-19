﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace MFDLabs.Hashicorp.VaultClient.V1.Commons
{
    /// <summary>
    /// Represents the list of keys.
    /// </summary>
    public class ListInfo
    {
        /// <summary>
        /// Gets or sets the keys.
        /// </summary>
        /// <value>
        /// The keys.
        /// </value>
        [JsonProperty("keys")]
        public IEnumerable<string> Keys { get; set; }
    }
}
