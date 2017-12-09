using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace AcidChicken.Samurai.Components
{
    using static Program;

    public static class NotificationManager
    {
        public static HashSet<SocketTextChannel> Channels { get; set; } = new HashSet<SocketTextChannel>();

        public static Task InitAsync()
        {
            Channels = Channels.Union(ApplicationConfig.NotificationChannels.Select(x => (SocketTextChannel)DiscordClient.GetChannel(x))).ToHashSet();
            return Task.CompletedTask;
        }
    }
}
