using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AcidChicken.Samurai.Tasks
{
    using static Program;
    using Assets;

    public static class MonitorManager
    {
        public static SocketTextChannel Channel { get; set; }

        public static Dictionary<string, IPStatus> Statuses { get; set; } = new Dictionary<string, IPStatus>();

        public static Dictionary<string, string> Targets { get; set; } = new Dictionary<string, string>();

        public static bool AddMonitor(string name, string hostname) => Targets.TryAdd(name, hostname) && Statuses.TryAdd(name, IPStatus.Unknown);

        public static bool DeleteMonitor(string name) => Targets.Remove(name) && Statuses.Remove(name);

        public static Task WorkAsync() => WorkAsync(default);

        public static async Task WorkAsync(CancellationToken token)
        {
            Channel = (SocketTextChannel)DiscordClient.GetChannel(Config.MonitorChannel);
            Targets = Targets.Union(Config.Targets).ToDictionary(x => x.Key, x => x.Value);
            foreach (var target in Config.Targets)
            {
                Targets.TryAdd(target.Key, target.Value);
            }
            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll
                (
                    Task.WhenAll(Statuses.Select(x => SendStatusChangeAsync(x.Key, x.Value))),
                    Task.Delay(60000)
                ).ConfigureAwait(false);
            }
        }

        public static async Task SendStatusChangeAsync(string name, IPStatus lastStatus = IPStatus.Unknown)
        {
            try
            {
                var ping = new Ping();
                var reply = await ping.SendPingAsync(Targets[name], 60000).ConfigureAwait(false);
                if (reply.Status == lastStatus)
                {
                    await Channel.SendMessageAsync
                    (
                        text: "",
                        embed:
                            new EmbedBuilder()
                                .WithTitle("状態変化")
                                .WithDescription($"{name}の状態が変化しました。")
                                .WithCurrentTimestamp()
                                .WithColor(reply.Status == IPStatus.Success ? Colors.Green : Colors.Red)
                                .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                                .AddInlineField("変化前", lastStatus)
                                .AddInlineField("変化後", reply.Status)
                                .AddInlineField("応答速度", $"{reply.RoundtripTime:#,0}ms")
                    ).ConfigureAwait(false);
                }
                await LogAsync(new LogMessage(LogSeverity.Verbose, "MonitorManager", $"{name}({Targets[name]} status is updated from {lastStatus} to {reply.Status})")).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await LogAsync(new LogMessage(LogSeverity.Error, "MonitorManager", ex.Message, ex)).ConfigureAwait(false);
            }
        }
    }
}
