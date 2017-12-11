using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Models
{
    [JsonObject]
    public class Config
    {
        [JsonProperty("discord_token")]
        public string DiscordToken { get; set; } = "";

        [JsonProperty("notification_channels")]
        public HashSet<ulong> NotificationChannels { get; set; } = new HashSet<ulong>();

        // [JsonProperty("rollbar_environment")]
        // public string RollbarEnvironment { get; set; } = "production";

        // [JsonProperty("rollbar_token")]
        // public string RollbarConfig { get; set; } = "";

        [JsonProperty("targets")]
        public Dictionary<string, Monitor> Monitors { get; set; } = new Dictionary<string, Monitor>();
    }

    [JsonObject]
    public class Monitor
    {
        public Monitor(string hostname = "", IPStatus lastStatus = IPStatus.Unknown)
        {
            Hostname = hostname;
            LastStatus = lastStatus;
        }

        [JsonProperty("hostname")]
        public string Hostname { get; set; } = "";

        [JsonProperty("last_status")]
        public IPStatus LastStatus { get; set; } = IPStatus.Unknown;
    }
}
