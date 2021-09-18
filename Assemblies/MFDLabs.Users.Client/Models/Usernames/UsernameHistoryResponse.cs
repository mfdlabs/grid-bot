using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Usernames
{
    public class UsernameHistoryResponse
    {
        /// <summary>A past username belonging to a particular userId</summary>
        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}
