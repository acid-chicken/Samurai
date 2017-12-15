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

        [JsonProperty("queue")]
        public List<TipQueue> Queue { get; set; } = new List<TipQueue>();

        // [JsonProperty("rollbar_environment")]
        // public string RollbarEnvironment { get; set; } = "production";

        // [JsonProperty("rollbar_token")]
        // public string RollbarConfig { get; set; } = "";

        [JsonProperty("rpc_password")]
        public string RpcPassword { get; set; } = "";

        [JsonProperty("rpc_server")]
        public string RpcServer { get; set; } = "http://127.0.0.1:9305";

        [JsonProperty("rpc_user")]
        public string RpcUser { get; set; } = "";

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

    [JsonObject]
    public class TipQueue
    {
        public TipQueue(ulong from, ulong to, DateTimeOffset limit, decimal amount)
        {
            From = from;
            To = to;
            Limit = limit;
            Amount = amount;
        }

        [JsonProperty("from")]
        public ulong From { get; set; }

        [JsonProperty("to")]
        public ulong To { get; set; }

        [JsonProperty("limit")]
        public DateTimeOffset Limit { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }
}
