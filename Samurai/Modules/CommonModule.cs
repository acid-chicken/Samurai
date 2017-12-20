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
    using Components;
    using Models;
    using Tasks;

    [Group(""), Summary("汎用モジュールです。")]
    public class CommonModule : ModuleBase
    {
        [Command("help"), Summary("コマンドのヘルプを表示します。"), Alias("ヘルプ", "?")]
        public async Task HelpAsync([Summary("対象のコマンド")] string name = null, [Remainder] string comment = null)
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
                                            builder.Insert(0, ' ');
                                            builder.Insert(0, x.Module.Name);
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
                            .WithFooter(EmbedManager.CurrentFooter)
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
                            {
                                Fields = new List<EmbedFieldBuilder>()
                                {
                                    new EmbedFieldBuilder()
                                        .WithName("~~――~~")
                                        .WithValue("*引数*")
                                }
                                .Concat(command.Parameters.Select
                                (x =>
                                    new EmbedFieldBuilder()
                                        .WithName(x.Name)
                                        .WithValue($"**{x.Summary}**\n既定値`{(x.DefaultValue ?? "null")}`\n型`{x.Type.Name}`{(x.IsOptional ? "\n(オプション)" : "")}{(x.IsMultiple ? "\n(複数指定可)" : "")}{(x.IsRemainder ? "\n(余白許容)" : "")}")
                                        .WithIsInline(true)
                                ))
                                .Concat(new List<EmbedFieldBuilder>
                                {
                                    new EmbedFieldBuilder()
                                        .WithName("~~―――――~~")
                                        .WithValue("*エイリアス*")
                                })
                                .Concat(command.Aliases.Select
                                (x =>
                                    new EmbedFieldBuilder()
                                        .WithName(x)
                                        .WithValue("\u200b")
                                        .WithIsInline(true)
                                ))
                                .ToList()
                            }
                                .WithTitle(command.Name)
                                .WithDescription(command.Summary)
                                .WithCurrentTimestamp()
                                .WithColor(Colors.Blue)
                                .WithFooter(EmbedManager.CurrentFooter)
                                .WithAuthor(Context.User)
                    ).ConfigureAwait(false);
                }
            }
        }

        [Command("prefix"), Summary("プレフィックスを設定します。"), RequireContext(ContextType.Guild)]
        public async Task PrefixAsync([Summary("新しいプレフィックス")] string prefix, [Remainder] string comment = null)
        {
            if (ApplicationConfig.Managers.Contains(Context.User.Id))
            {
                var before = ModuleManager.Prefix;
                if (ApplicationConfig.PrefixOverrides.ContainsKey(Context.Guild.Id))
                {
                    before = ApplicationConfig.PrefixOverrides[Context.Guild.Id];
                    ApplicationConfig.PrefixOverrides[Context.Guild.Id] = prefix;
                }
                else
                {
                    ApplicationConfig.PrefixOverrides.Add(Context.Guild.Id, prefix);
                }
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("プレフィック設定完了")
                            .WithDescription("プレフィックを設定しました。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                            .AddInlineField("設定前", before)
                            .AddInlineField("設定後", prefix)
                            .AddInlineField("対象サーバー", Context.Guild.Name)
                ).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("プレフィック設定失敗")
                            .WithDescription("プレフィックを設定する権限がありません。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
        }

        [Command("save"), Summary("Botの最新状態を安全に保存します。")]
        public async Task SaveAsync([Remainder] string comment = null)
        {
            if (ApplicationConfig.Managers.Contains(Context.User.Id))
            {
                await SaveBotConfigAsync().ConfigureAwait(false);
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("保存完了")
                            .WithDescription("最新のデータを全て保存しました。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("保存失敗")
                            .WithDescription("Botのデータを保存する権限がありません。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
        }

        [Command("set"), Summary("ユーザー設定を編集します。")]
        public Task SetAsync([Summary("設定キー")] string key, [Summary("設定値")] string value, [Remainder] string comment = null)
        {
            if (ApplicationConfig.Settings.ContainsKey(Context.User.Id))
            {
                if (ApplicationConfig.Settings[Context.User.Id].ContainsKey(key))
                {
                    ApplicationConfig.Settings[Context.User.Id][key] = value;
                }
                else
                {
                    ApplicationConfig.Settings[Context.User.Id].Add(key, value);
                }
            }
            else
            {
                ApplicationConfig.Settings.Add(Context.User.Id, new Dictionary<string, string>(){ { key, value } });
            }
            return Task.CompletedTask;
        }

        [Command("stop"), Summary("Botを安全に終了します。")]
        public async Task StopAsync([Remainder] string comment = null)
        {
            if (ApplicationConfig.Managers.Contains(Context.User.Id))
            {
                await SaveBotConfigAsync().ConfigureAwait(false);
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("終了準備完了")
                            .WithDescription("Botを終了する準備が完了しました。Botを終了しています。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
                CancellationTokenSource.Cancel();
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("終了準備失敗")
                            .WithDescription("Botを終了する権限がありません。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
        }

        [Command("version"), Summary("バージョン情報を表示します。"), Alias("ver")]
        public async Task VersionAsync([Remainder] string comment = null)
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
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .WithThumbnailUrl(DiscordClient.CurrentUser.GetAvatarUrl())
                        .AddInlineField("バージョン", ((AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute))).InformationalVersion)
                        .AddInlineField("著作権情報", ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyCopyrightAttribute))).Copyright)
            ).ConfigureAwait(false);
        }
    }
}
