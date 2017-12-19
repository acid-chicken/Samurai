using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AcidChicken.Samurai.Discord.Modules
{
    using static Program;
    using Assets;
    using Components;

    public static class ModuleManager
    {
        public const string Prefix = "./";

        public static readonly string[] NonIgnorable = new []
        {
            "tip",
            "send"
        };

        public static ulong Busy;

        static ModuleManager()
        {
            ServiceConfig = new CommandServiceConfig();
            Service = new CommandService();
        }

        public static CommandService Service { get; }

        public static CommandServiceConfig ServiceConfig { get; }

        public static Task<IEnumerable<ModuleInfo>> InstallAsync()
        {
            DiscordClient.MessageReceived += (message) => Task.WhenAny(HandleCommandAsync(message), Task.Delay(0));
            return Service.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public static async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var position = 0;
            var message = socketMessage as SocketUserMessage;
            var guildChannel = message.Channel as IGuildChannel;
            if (message == null ||
                !((message.HasMentionPrefix(DiscordClient.CurrentUser, ref position)) ||
                message.HasStringPrefix
                (
                    guildChannel != null &&
                    ApplicationConfig.PrefixOverrides.ContainsKey(guildChannel.GuildId) ?
                    ApplicationConfig.PrefixOverrides[guildChannel.GuildId] :
                    Prefix,
                    ref position
                )) ||
                (
                    !NonIgnorable.Contains(message.Content.Substring(position).Split(' ')[0]) &&
                    (guildChannel != null && ((guildChannel as ITextChannel)?.Topic?.Contains("./ignore") ?? false))
                )) return;
            var context = new CommandContext(DiscordClient, message);
            using (var typing = context.Channel.EnterTypingState())
            {
                if (++Busy > ApplicationConfig.Busy)
                {
                    await context.Channel.SendMessageAsync
                    (
                        text: context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle("ビジー状態")
                                .WithDescription("現在Botが混み合っています。時間を空けてから再度お試し下さい。")
                                .WithCurrentTimestamp()
                                .WithColor(Colors.Orange)
                                .WithColor(Colors.Red)
                                .WithFooter(EmbedManager.CurrentFooter)
                                .WithAuthor(context.User)
                    );
                }
                else
                {
                    var result = await Service.ExecuteAsync(context, position);
                    if (!result.IsSuccess)
                    {
                        await context.Channel.SendMessageAsync
                        (
                            text: context.User.Mention,
                            embed:
                                new EmbedBuilder()
                                    .WithTitle("コマンドエラー")
                                    .WithDescription(result.ErrorReason)
                                    .WithCurrentTimestamp()
                                    .WithColor(Colors.Red)
                                    .WithFooter(EmbedManager.CurrentFooter)
                                    .WithAuthor(context.User)
                        );
                    }
                }
            }
            Busy--;
        }
    }
}
