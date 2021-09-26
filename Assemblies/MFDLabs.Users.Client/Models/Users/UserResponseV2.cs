using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    [DataContract]
    public class UserResponseV2
    {
        [DataMember(Name = "description", IsRequired = false)]
        [JsonProperty("description", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Description { get; set; }

        [DataMember(Name = "created", IsRequired = false)]
        [JsonProperty("created", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? Created { get; set; }

        [DataMember(Name = "isBanned", IsRequired = false)]
        [JsonProperty("isBanned", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsBanned { get; set; }

        [DataMember(Name = "externalAppDisplayName", IsRequired = false)]
        [JsonProperty("externalAppDisplayName", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string ExternalAppDisplayName { get; set; }

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
