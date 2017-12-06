using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AcidChicken.Samurai.Modules
{
    public class CommonModule : ModuleBase
    {
        [Command("help"), Summary("コマンドのヘルプを表示します。"), Alias("ヘルプ", "?")]
        public async Task HelpAsync([Remainder, Summary("対象のコマンド")] string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                        {
                            Fields =
                                ModuleManager.Service.Commands
                                    .Select
                                    (x =>
                                        new EmbedFieldBuilder()
                                            .WithName(x.Name)
                                            .WithValue(x.Summary)
                                            .WithIsInline(true)
                                    )
                                    .ToList()
                        }
                            .WithTitle("利用可能コマンド")
                            .WithDescription("現在利用可能なコマンドを下記に列挙します。")
                            .WithCurrentTimestamp()
                            .WithColor(Color.Blue)
                            .WithFooter(Program.DiscordClient.CurrentUser.Username, Program.DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(Context.User)
                );
            }
            else
            {
                var command = ModuleManager.Service.Commands.FirstOrDefault(x => x.Name == name || x.Aliases.Contains(name));
                if (command == null)
                {
                    throw new Exception($"`{name}`に一致する名称、もしくはエイリアスを持つコマンドが見つかりませんでした。");
                }
                else
                {
                    await ReplyAsync
                    (
                        message: Context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle(command.Name)
                                .WithDescription(command.Summary)
                                .WithCurrentTimestamp()
                                .WithColor(Color.Green)
                                .WithFooter(Program.DiscordClient.CurrentUser.Username, Program.DiscordClient.CurrentUser.GetAvatarUrl())
                                .WithAuthor(Context.User)
                    );
                }
            }
        }
    }
}
