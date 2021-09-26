using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.Usernames
{
    [DataContract]
    public class UsernameHistoryResponse
    {
        /// <summary>A past username belonging to a particular userId</summary>
        [DataMember(Name = "name", IsRequired = false)]
        [JsonProperty("name", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
    }
}
