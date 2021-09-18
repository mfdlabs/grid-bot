using Newtonsoft.Json;
using System.Collections.Generic;

namespace MFDLabs.Users.Client.Models.Users
{
    /// <summary>Request model for getting users by usernames.</summary>
    public class MultiGetByUsernameRequest
    {
        // <summary>The usernames.</summary>
        [JsonProperty("usernames", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<string> Usernames { get; set; }

        /// <summary>Whether or not the response should exclude banned users</summary>
        [JsonProperty("excludeBannedUsers", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExcludeBannedUsers { get; set; }
    }
}
