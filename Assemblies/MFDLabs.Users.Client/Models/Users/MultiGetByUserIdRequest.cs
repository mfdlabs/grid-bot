using Newtonsoft.Json;
using System.Collections.Generic;

namespace MFDLabs.Users.Client.Models.Users
{
    /// <summary>Request model for getting users by ids.</summary>
    public class MultiGetByUserIdRequest
    {
        /// <summary>The user ids.</summary>
        [JsonProperty("userIds", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<long> UserIds { get; set; }

        /// <summary>Whether or not the response should exclude banned users</summary>
        [JsonProperty("excludeBannedUsers", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExcludeBannedUsers { get; set; }
    }
}
