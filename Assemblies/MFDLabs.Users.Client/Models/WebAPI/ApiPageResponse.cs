using Newtonsoft.Json;
using System.Collections.Generic;

namespace MFDLabs.Users.Client.Models.WebAPI
{
    public class ApiPageResponse<T>
    {
        [JsonProperty("previousPageCursor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string PreviousPageCursor { get; set; }

        [JsonProperty("nextPageCursor", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public string NextPageCursor { get; set; }

        [JsonProperty("data", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<T> Data { get; set; }
    }
}
