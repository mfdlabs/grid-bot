using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    [DataContract]
    public class UserStatusResponse
    {
        [DataMember(Name = "status", IsRequired = false)]
        [JsonProperty("status", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
    }
}
