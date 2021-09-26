using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    /// <summary>Request model for getting users by ids.</summary>
    [DataContract]
    public class MultiGetByUserIdRequest
    {
        /// <summary>The user ids.</summary>
        [DataMember(Name = "userIds", IsRequired = true)]
        [JsonProperty("userIds", Required = Required.Always, NullValueHandling = NullValueHandling.Include)]
        public ICollection<long> UserIds { get; set; }

        /// <summary>Whether or not the response should exclude banned users</summary>
        [DataMember(Name = "excludeBannedUsers", IsRequired = false)]
        [JsonProperty("excludeBannedUsers", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExcludeBannedUsers { get; set; }
    }
}
