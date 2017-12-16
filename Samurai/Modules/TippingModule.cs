using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace AcidChicken.Samurai.Modules
{
    using static Program;
    using Assets;
    using Components;
    using Tasks;

    [Group(""), Summary("投げ銭モジュールです。")]
    public class TippingModule : ModuleBase
    {
        [Command("balance"), Summary("残高を表示します。"), Alias("残高")]
        public async Task BalanceAsync()
        {
            var account = TippingManager.GetAccountName(Context.User);
            var address = await TippingManager.EnsureAccountAsync(account).ConfigureAwait(false);
            await Task.WhenAll(TippingManager.Queue.Where(x => x.To == Context.User.Id).Select(async x =>
            {
                try
                {
                    var from = DiscordClient.GetUser(x.From);
                    var txid = await TippingManager.InvokeMethodAsync("sendfrom", TippingManager.GetAccountName(from), address, x.Amount).ConfigureAwait(false);
                    var dequeued = await TippingManager.DequeueAsync(x).ConfigureAwait(false);
                    await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingModule", $"Sent {x.Amount} ZNY from {from.Username}#{from.Discriminator} to {Context.User.Username}#{Context.User.Discriminator}."));
                }
                catch (Exception ex)
                {
                    await RequestLogAsync(new LogMessage(LogSeverity.Error, "TippingModule", ex.Message, ex));
                }
            }));
            var balance = await TippingManager.InvokeMethodAsync("getbalance", account).ConfigureAwait(false);
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("残高")
                        .WithDescription($"{balance} ZNY")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Blue)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
            ).ConfigureAwait(false);
        }

        [Command("deposit"), Summary("入金用アドレスを表示します。")]
        public async Task DepositAsync()
        {
            var address = await TippingManager.EnsureAccountAsync(TippingManager.GetAccountName(Context.User)).ConfigureAwait(false);
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("入金用アドレス")
                        .WithDescription($"```{address}```")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Blue)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
            ).ConfigureAwait(false);
        }

        [Command("send"), Summary("指定されたユーザーに送金します。"), Alias("送金")]
        public async Task SendAsync([Summary("送り先のユーザー")] IUser user, [Summary("金額")] decimal amount)
        {
            var account = TippingManager.GetAccountName(Context.User);
            var address = await TippingManager.EnsureAccountAsync(TippingManager.GetAccountName(user)).ConfigureAwait(false);
            var txid = await TippingManager.InvokeMethodAsync("sendfrom", account, address, amount).ConfigureAwait(false);
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("送金完了")
                        .WithDescription($"{user.Mention} に送金しました。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Green)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .WithThumbnailUrl(user.GetAvatarUrl())
                        .AddInlineField("金額", $"{amount} ZNY")
                        .AddInlineField("トランザクションID", txid)
            ).ConfigureAwait(false);
            var dm = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            await dm.SendMessageAsync
            (
                text: user.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("送金通知")
                        .WithDescription($"{Context.User.Mention} からの送金を受け取りました。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Orange)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(user)
                        .WithThumbnailUrl(Context.User.GetAvatarUrl())
                        .AddInlineField("金額", $"{amount} ZNY")
                        .AddInlineField("受取期限", txid)
            ).ConfigureAwait(false);
            await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingModule", $"Sent {amount} ZNY from {Context.User.Username}#{Context.User.Discriminator} to {user.Username}#{user.Discriminator}.")).ConfigureAwait(false);
        }

        [Command("tip"), Summary("指定されたユーザーに投げ銭します。"), Alias("投銭")]
        public async Task TipAsync([Summary("送り先のユーザー")] IUser user, [Summary("金額")] decimal amount)
        {
            var limit = DateTimeOffset.Now.AddDays(3);
            await TippingManager.EnqueueAsync(new Models.TipQueue(Context.User.Id, user.Id, limit, amount));
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("投げ銭完了")
                        .WithDescription($"{user.Mention} に投げ銭を行いました。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Green)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .WithThumbnailUrl(user.GetAvatarUrl())
                        .AddInlineField("金額", $"{amount} ZNY")
                        .AddInlineField("受取期限", limit)
            ).ConfigureAwait(false);
            var dm = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            await dm.SendMessageAsync
            (
                text: user.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("投げ銭通知")
                        .WithDescription($"{Context.User.Mention} から投げ銭が届いています。受取期限までに`{ModuleManager.Prefix}balance`を実行することで投げ銭を受け取れます。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Orange)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(user)
                        .WithThumbnailUrl(Context.User.GetAvatarUrl())
                        .AddInlineField("金額", $"{amount} ZNY")
                        .AddInlineField("受取期限", limit)
            ).ConfigureAwait(false);
        }
    }
}
