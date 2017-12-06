using System;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Models
{
    [JsonObject]
    public class Config
    {
        [JsonProperty("discord_token")]
        public string DiscordToken { get; set; } = "";
    }
}
