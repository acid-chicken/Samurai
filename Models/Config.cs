using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Models
{
    [JsonObject]
    public class Config
    {
        [JsonProperty("discord_token")]
        public string DiscordToken { get; set; } = "";

        [JsonProperty("monitor_channel")]
        public ulong MonitorChannel { get; set; } = 0;

        [JsonProperty("targets")]
        public Dictionary<string, string> Targets { get; set; } = new Dictionary<string, string>();
    }
}
