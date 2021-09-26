using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace MFDLabs.Users.Client.Models.WebAPI
{
    [DataContract]
    public class ApiPageResponse<T>
    {
        [DataMember(Name = "previousPageCursor", IsRequired = false)]
        [JsonProperty("previousPageCursor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string PreviousPageCursor { get; set; }

        [DataMember(Name = "nextPageCursor", IsRequired = false)]
        [JsonProperty("nextPageCursor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string NextPageCursor { get; set; }

        [DataMember(Name = "data", IsRequired = false)]
        [JsonProperty("data", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<T> Data { get; set; }
    }
}
