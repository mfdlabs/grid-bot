using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.UserSearch
{
    /// <summary>A user response model specific to getting a user from user search.</summary>
    [DataContract]
    public class UserSearchUserResponse
    {
        /// <summary>Previous usernames for a user.</summary>
        [DataMember(Name = "previousUsernames", IsRequired = false)]
        [JsonProperty("previousUsernames", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<string> PreviousUsernames { get; set; }

        [DataMember(Name = "id", IsRequired = false)]
        [JsonProperty("id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public long? ID { get; set; }

        [DataMember(Name = "name", IsRequired = false)]
        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [DataMember(Name = "displayName", IsRequired = false)]
        [JsonProperty("displayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
    }
}
