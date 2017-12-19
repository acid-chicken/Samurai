using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AcidChicken.Samurai.Discord.Models
{
    [JsonObject]
    public class RpcResponse<T>
    {
        [JsonProperty("result")]
        public T Result { get; set; }

        [JsonProperty("error")]
        public RpcError Error { get; set; }

        [JsonProperty("id")]
        public object Id { get; set; }
    }

    public class RpcError
    {
        [JsonProperty("code")]
        public short Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
