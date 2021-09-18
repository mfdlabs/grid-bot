using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    /// <summary>A response model specific to multi-get user by name.</summary>
    public class MultiGetUserByNameResponse
    {
        /// <summary>The username the user was requested with.</summary>
        [JsonProperty("requestedUsername", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string RequestedUsername { get; set; }

        [JsonProperty("id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public long? ID { get; set; }

        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("displayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
    }
}
