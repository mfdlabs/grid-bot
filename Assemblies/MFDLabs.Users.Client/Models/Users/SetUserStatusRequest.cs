using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    public class SetUserStatusRequest
    {
        [JsonProperty("status", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
    }
}
