using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AcidChicken.Samurai.Modules
{
    using Assets;

    public static class ModuleManager
    {
        public const string Prefix = "./";

        static ModuleManager()
        {
            ServiceConfig = new CommandServiceConfig();
            Service = new CommandService();
        }

        public static CommandService Service { get; }

        public static CommandServiceConfig ServiceConfig { get; }

        public static Task<IEnumerable<ModuleInfo>> InstallAsync()
        {
            Program.DiscordClient.MessageReceived += HandleCommandAsync;
            return Service.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public static async Task HandleCommandAsync(SocketMessage socketMessage)
        {
            var position = 0;
            var message = socketMessage as SocketUserMessage;
            if (message == null || !((message.HasMentionPrefix(Program.DiscordClient.CurrentUser, ref position)) || (message.HasStringPrefix(Prefix, ref position)))) return;
            var context = new CommandContext(Program.DiscordClient, message);
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
                            .WithColor(Color.Red)
                            .WithFooter(Program.DiscordClient.CurrentUser.Username, Program.DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(context.User)
                );
            }
        }
    }
}
