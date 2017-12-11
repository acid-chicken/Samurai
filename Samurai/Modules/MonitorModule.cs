using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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

    [Group("monitor"), Summary("モニターを管理します。"), Alias("m")]
    public class MonitorModule : ModuleBase
    {
        [Command("add"), Summary("モニターを追加します。"), Alias("create", "+")]
        public async Task AddAsync([Summary("モニターの名前")] string name, [Remainder, Summary("モニターのホスト名")] string hostname)
        {
            if (MonitorManager.AddMonitor(name, hostname) && ApplicationConfig.Monitors.TryAdd(name, new Monitor(hostname)))
            {
                await SaveConfigAsync(ApplicationConfig).ConfigureAwait(false);
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("モニター追加成功")
                            .WithDescription("モニターの追加に成功しました。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                            .AddInlineField("モニター名", name)
                            .AddInlineField("ホスト名", hostname)
                ).ConfigureAwait(false);
                NotificationManager.Channels.Select
                (async x =>
                    await x.SendMessageAsync
                    (
                        text: Context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle("モニター追加")
                                .WithDescription($"{Context.User.Username}#{Context.User.Discriminator}がモニターを追加しました。")
                                .WithCurrentTimestamp()
                                .WithColor(Colors.Blue)
                                .WithFooter(EmbedManager.CurrentFooter)
                                .WithAuthor(Context.User)
                                .AddInlineField("モニター名", name)
                                .AddInlineField("ホスト名", hostname)
                    ).ConfigureAwait(false)
                );
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("モニター追加失敗")
                            .WithDescription("モニターの追加に失敗しました。既に同名のモニターが存在する可能性があります。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                            .AddInlineField("モニター名", name)
                            .AddInlineField("ホスト名", hostname)
                ).ConfigureAwait(false);
            }
        }

        [Command("delete"), Summary("モニターを削除します。"), Alias("remove", "-")]
        public async Task DeleteAsync([Remainder, Summary("削除するモニター")] string name)
        {
            if (MonitorManager.DeleteMonitor(name) && ApplicationConfig.Monitors.Remove(name))
            {
                await SaveConfigAsync(ApplicationConfig).ConfigureAwait(false);
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("モニター削除成功")
                            .WithDescription("モニターの削除に成功しました。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                            .AddInlineField("モニター名", name)
                ).ConfigureAwait(false);
                NotificationManager.Channels.Select
                (async x =>
                    await x.SendMessageAsync
                    (
                        text: Context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle("モニター削除")
                                .WithDescription($"{Context.User.Username}#{Context.User.Discriminator}がモニターを削除しました。")
                                .WithCurrentTimestamp()
                                .WithColor(Colors.Blue)
                                .WithFooter(EmbedManager.CurrentFooter)
                                .WithAuthor(Context.User)
                                .AddInlineField("モニター名", name)
                    ).ConfigureAwait(false)
                );
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("モニター削除失敗")
                            .WithDescription("モニターの削除に失敗しました。既にモニターが存在しない可能性があります。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                            .AddInlineField("モニター名", name)
                ).ConfigureAwait(false);
            }
        }

        [Command("information"), Summary("モニターの情報を表示します。"), Alias("info", "show", "lookup")]
        public async Task InformationAsync([Remainder, Summary("対象のモニター")] string name = null)
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
                                MonitorManager.Targets
                                    .Select
                                    (x =>
                                        new EmbedFieldBuilder()
                                            .WithName(x.Key)
                                            .WithValue(x.Value)
                                            .WithIsInline(true)
                                    )
                                    .ToList()
                        }
                            .WithTitle("モニター")
                            .WithDescription("現在設定されているモニターを下記に列挙します。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Blue)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
            else
            {
                if (MonitorManager.Targets.TryGetValue(name, out string hostname) && MonitorManager.Statuses.TryGetValue(name, out IPStatus lastStatus))
                {
                    await ReplyAsync
                    (
                        message: Context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle("モニター")
                                .WithDescription("下記に情報を表示します。")
                                .WithCurrentTimestamp()
                                .WithColor(Colors.Blue)
                                .WithFooter(EmbedManager.CurrentFooter)
                                .WithAuthor(Context.User)
                                .AddInlineField("モニター名", name)
                                .AddInlineField("ホスト名", hostname)
                                .AddInlineField("状況", lastStatus)
                    ).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync
                    (
                        message: Context.User.Mention,
                        embed:
                            new EmbedBuilder()
                                .WithTitle("コマンドエラー")
                                .WithDescription($"`{name}`に一致する名前のモニターが見つかりませんでした。誤字脱字や大文字小文字を確認して下さい。")
                                .WithCurrentTimestamp()
                                .WithColor(Colors.Red)
                                .WithFooter(EmbedManager.CurrentFooter)
                                .WithAuthor(Context.User)
                    ).ConfigureAwait(false);
                }
            }
        }
    }
}
