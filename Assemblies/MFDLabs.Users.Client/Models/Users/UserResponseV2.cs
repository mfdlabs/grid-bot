using Newtonsoft.Json;
using System;

namespace MFDLabs.Users.Client.Models.Users
{
    public class UserResponseV2
    {
        [JsonProperty("description", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [JsonProperty("created", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Created { get; set; }

        [JsonProperty("isBanned", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsBanned { get; set; }

        [JsonProperty("externalAppDisplayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string ExternalAppDisplayName { get; set; }

        [JsonProperty("id", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public long? ID { get; set; }

        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        [JsonProperty("displayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string DisplayName { get; set; }
    }
}
