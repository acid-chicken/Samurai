using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace AcidChicken.Samurai.Modules
{
    using static Program;
    using Assets;
    using Components;
    using Tasks;

    [Group("notification"), Summary("通知管理モジュールです。"), Alias("notify", "n")]
    public class NotificationModule : ModuleBase
    {
        [Command("add"), Summary("通知チャンネルを追加します。"), Alias("create", "+")]
        public async Task AddAsync([Summary("対象のチャンネル")] ITextChannel channel)
        {
            if (NotificationManager.Channels.Add((SocketTextChannel)channel))
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("チャンネル追加成功")
                            .WithDescription("通知チャンネルの追加に成功しました。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(Context.User)
                            .AddInlineField("チャンネル", $"{channel.Name}({channel.Id})")
                            .AddInlineField("サーバー", $"{channel.Guild.Name}({channel.GuildId})")
                ).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("チャンネル追加失敗")
                            .WithDescription("通知チャンネルの追加に失敗しました。既にチャンネルが登録されている可能性があります。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(Context.User)
                            .AddInlineField("チャンネル", $"{channel.Name}({channel.Id})")
                            .AddInlineField("サーバー", $"{channel.Guild.Name}({channel.GuildId})")
                ).ConfigureAwait(false);
            }
        }

        [Command("delete"), Summary("通知チャンネルを削除します。"), Alias("remove", "rm", "-")]
        public async Task DeleteAsync([Summary("対象のチャンネル")] ITextChannel channel)
        {
            if (NotificationManager.Channels.Remove((SocketTextChannel)channel))
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("チャンネル削除成功")
                            .WithDescription("通知チャンネルの削除に成功しました。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(Context.User)
                            .AddInlineField("チャンネル", $"{channel.Name}({channel.Id})")
                            .AddInlineField("サーバー", $"{channel.Guild.Name}({channel.GuildId})")
                ).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("チャンネル削除失敗")
                            .WithDescription("通知チャンネルの削除に失敗しました。既にチャンネルが削除されている可能性があります。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                            .WithAuthor(Context.User)
                            .AddInlineField("チャンネル", $"{channel.Name}({channel.Id})")
                            .AddInlineField("サーバー", $"{channel.Guild.Name}({channel.GuildId})")
                ).ConfigureAwait(false);
            }
        }

        [Command("information"), Summary("通知チャンネルの情報を表示します。"), Alias("info", "show", "lookup")]
        public async Task InformationAsync()
        {
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                    {
                        Fields =
                            NotificationManager.Channels
                                .Select
                                (x =>
                                    new EmbedFieldBuilder()
                                        .WithName($"{x.Name}({x.Id})")
                                        .WithValue($"{x.Guild.Name}({x.Guild.Id})")
                                        .WithIsInline(true)
                                )
                                .ToList()
                    }
                        .WithTitle("通知チャンネル")
                        .WithDescription("現在設定されている通知チャンネルを下記に列挙します。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Blue)
                        .WithFooter(DiscordClient.CurrentUser.Username, DiscordClient.CurrentUser.GetAvatarUrl())
                        .WithAuthor(Context.User)
            ).ConfigureAwait(false);
        }
    }
}
