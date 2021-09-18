using Newtonsoft.Json;

namespace Discord.API
{
    public class InviteVanity
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        [JsonProperty("uses")]
        public int Uses { get; set; }
    }
}
