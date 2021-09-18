using Newtonsoft.Json;
using System.Collections.Generic;

namespace MFDLabs.Users.Client.Models.UserSearch
{
    /// <summary>A user response model specific to getting a user from user search.</summary>
    public class UserSearchUserResponse
    {
        /// <summary>Previous usernames for a user.</summary>
        [JsonProperty("previousUsernames", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<string> PreviousUsernames { get; set; }

        [JsonProperty("id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public long? ID { get; set; }

        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("displayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
    }
}
