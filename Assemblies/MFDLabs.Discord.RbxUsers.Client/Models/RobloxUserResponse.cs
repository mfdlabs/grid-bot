using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MFDLabs.Discord.RbxUsers.Client.Models
{
    [DataContract]
    public class RobloxUserResponse
    {
        [DataMember(Name = "status", IsRequired = true)]
        [JsonProperty("status", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public ResolutionStatusType Status { get; set; }

        [DataMember(Name = "robloxUsername", IsRequired = false)]
        [JsonProperty("robloxUsername", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Username { get; set; }

        [DataMember(Name = "robloxId", IsRequired = false)]
        [JsonProperty("robloxId", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public long Id { get; set; }

        [DataMember(Name = "errorCode", IsRequired = false)]
        [JsonProperty("errorCode", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public int ErrorCode { get; set; }

        [DataMember(Name = "error", IsRequired = false)]
        [JsonProperty("error", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorMessage { get; set; }
    }
}
