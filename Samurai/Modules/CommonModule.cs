using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AcidChicken.Samurai.Modules
{
    using static Program;
    using Assets;

    [Group, Summary("汎用モジュールです。")]
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
                                    .Select(x =>
                                    {
                                        var builder = new StringBuilder();
                                        var module = x.Module;
                                        while (!string.IsNullOrEmpty(module?.Name))
                                        {
                                            builder.Insert(0, $"{x.Module.Name} ");
                                            module = module.Parent;
                                        }
                                        builder.Append(x.Name);
                                        return
                                            new EmbedFieldBuilder()
                                                .WithName(builder.ToString())
                                                .WithValue(x.Summary)
                                                .WithIsInline(true);
                                    })
                                    .ToList()
                        }
                            .WithTitle("利用可能コマンド")
                            .WithDescription("現在利用可能なコマンドを下記に列挙します。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Blue)
                            .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
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
                                .WithColor(Colors.Green)
                                .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                                .WithAuthor(Context.User)
                    ).ConfigureAwait(false);
                }
            }
        }

        [Command("version"), Summary("バージョン情報を表示します。"), Alias("ver")]
        public async Task VersionAsync()
        {
            var assembly = Assembly.GetEntryAssembly();
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle(((AssemblyTitleAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyTitleAttribute))).Title)
                        .WithDescription(((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyDescriptionAttribute))).Description)
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Blue)
                        .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                        .WithAuthor(Context.User)
                        .AddInlineField("バージョン", ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute))).InformationalVersion)
                        .AddInlineField("著作権情報", ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute))).Copyright)
            ).ConfigureAwait(false);
        }
    }
}
