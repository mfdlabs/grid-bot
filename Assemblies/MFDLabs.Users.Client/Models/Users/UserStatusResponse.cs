using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Users
{
    public class UserStatusResponse
    {
        [JsonProperty("status", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Status { get; set; }
    }
}
