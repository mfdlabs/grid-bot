using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.WebAPI
{
    [DataContract]
    public class ApiArrayResponse<T>
    {
        [DataMember(Name = "data", IsRequired = false)]
        [JsonProperty("data", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<T> Data { get; set; }
    }
}
