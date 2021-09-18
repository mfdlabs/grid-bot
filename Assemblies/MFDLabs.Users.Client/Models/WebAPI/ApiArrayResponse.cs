using Newtonsoft.Json;
using System.Collections.Generic;

namespace MFDLabs.Users.Client.Models.WebAPI
{
    public class ApiArrayResponse<T>
    {
        [JsonProperty("data", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public ICollection<T> Data { get; set; }
    }
}
