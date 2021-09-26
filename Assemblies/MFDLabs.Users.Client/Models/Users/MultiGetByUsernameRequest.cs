using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    /// <summary>Request model for getting users by usernames.</summary>
    [DataContract]
    public class MultiGetByUsernameRequest
    {
        // <summary>The usernames.</summary>
        [DataMember(Name = "usernames", IsRequired = true)]
        [JsonProperty("usernames", Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<string> Usernames { get; set; }

        /// <summary>Whether or not the response should exclude banned users</summary>
        [DataMember(Name = "excludeBannedUsers", IsRequired = false)]
        [JsonProperty("excludeBannedUsers", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExcludeBannedUsers { get; set; }
    }
}
