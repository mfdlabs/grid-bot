using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    [DataContract]
    public class SetUserStatusRequest
    {
        [DataMember(Name = "status", IsRequired = true)]
        [JsonProperty("status", Required = Required.Always, NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
    }
}
