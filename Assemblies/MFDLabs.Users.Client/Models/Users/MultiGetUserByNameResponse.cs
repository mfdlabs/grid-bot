using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    /// <summary>A response model specific to multi-get user by name.</summary>
    [DataContract]
    public class MultiGetUserByNameResponse
    {
        /// <summary>The username the user was requested with.</summary>
        [DataMember(Name = "requestedUsername", IsRequired = false)]
        [JsonProperty("requestedUsername", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string RequestedUsername { get; set; }

        [DataMember(Name = "id", IsRequired = false)]
        [JsonProperty("id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public long? Id { get; set; }

        [DataMember(Name = "name", IsRequired = false)]
        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [DataMember(Name = "displayName", IsRequired = false)]
        [JsonProperty("displayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
    }
}
