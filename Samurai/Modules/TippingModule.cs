using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Newtonsoft.Json;

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
            var balance0 = await TippingManager.InvokeMethodAsync("getbalance", account, 0).ConfigureAwait(false);
            var balance1 = await TippingManager.InvokeMethodAsync("getbalance", account, 1).ConfigureAwait(false);
            var queued = TippingManager.Queue.Where(x => x.From == Context.User.Id).Sum(x => x.Amount);
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("残高")
                        .WithDescription($"{balance0} ZNY")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Blue)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .AddInlineField("利用可能", $"{decimal.Parse(balance0) - queued:N8} ZNY")
                        .AddInlineField("検証待ち", $"{balance1} ZNY")
                        .AddInlineField("受取待ち", $"{queued:N8} ZNY")
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

        [Command("rain"), Summary("条件を満たしたユーザー全員に均等に投げ銭します。端数で総金額が多少変動することがあります。"), Alias("撒金"), RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task RainAsync([Summary("金額")] decimal totalAmount)
        {
            var targets =
                JsonConvert
                    .DeserializeObject<Dictionary<string, decimal>>(await TippingManager.InvokeMethodAsync("listaccounts").ConfigureAwait(false))
                    .Where
                    (user =>
                        user.Key.StartsWith("discord:") &&
                        user.Value >= 10 &&
                        Context.Channel
                            .GetUsersAsync()
                            .Flatten()
                            .Result
                                .Select(x => x.Id)
                                .Contains(ulong.TryParse(new string(user.Key.Skip(8).ToArray()), out ulong result) ? result : 0)
                    )
                    .Select(x => (IUser)DiscordClient.GetUser(ulong.TryParse(new string(x.Key.Skip(8).ToArray()), out ulong result) ? result : 0))
                    .ToHashSet();
            targets.Remove(Context.User);
            if (targets.Any())
            {
                var limit = DateTimeOffset.Now.AddDays(3);
                var amount = totalAmount / targets.Count;
                var count = targets.Count;
                await Task.WhenAll(targets.Select(x => TippingManager.EnqueueAsync(new Models.TipQueue(Context.User.Id, x.Id, limit, amount))).Append(ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("撒き銭完了")
                            .WithDescription("撒き銭しました。DM通知は行われませんのでご注意下さい。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Green)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                            .AddInlineField("一人あたりの金額", $"{amount:N8} ZNY")
                            .AddInlineField("対象者数", $"{count} 人")
                            .AddInlineField("総金額", $"{amount * count:N8} ZNY")
                ))).ConfigureAwait(false);
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("撒き銭失敗")
                            .WithDescription("撒き銭に失敗しました。撒き銭できるユーザーがいません。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
        }

        [Command("send"), Summary("指定されたユーザーに送金します。"), Alias("送金")]
        public async Task SendAsync([Summary("送り先のユーザー")] IUser user, [Remainder, Summary("金額")] decimal amount)
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
                        .AddInlineField("金額", $"{amount:N8} ZNY")
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
                        .AddInlineField("金額", $"{amount:N8} ZNY")
                        .AddInlineField("受取期限", txid)
            ).ConfigureAwait(false);
            await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingModule", $"Sent {amount:N8} ZNY from {Context.User.Username}#{Context.User.Discriminator} to {user.Username}#{user.Discriminator}.")).ConfigureAwait(false);
        }

        [Command("tip"), Summary("指定されたユーザーに投げ銭します。"), Alias("投銭")]
        public async Task TipAsync([Summary("送り先のユーザー")] IUser user, [Remainder, Summary("金額")] decimal amount)
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
                        .AddInlineField("金額", $"{amount:N8} ZNY")
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
                        .AddInlineField("金額", $"{amount:N8} ZNY")
                        .AddInlineField("受取期限", limit)
            ).ConfigureAwait(false);
        }

        [Command("withdraw"), Summary("指定されたアドレスに出金します。金額を指定しなかった場合は全額出金されます。"), Alias("出金")]
        public async Task WithDrawAsync([Summary("送り先のアドレス")] string address, [Remainder, Summary("金額")] decimal amount = decimal.MinusOne)
        {
            var account = TippingManager.GetAccountName(Context.User);
            await TippingManager.EnsureAccountAsync(account).ConfigureAwait(false);
            var txid = await TippingManager.InvokeMethodAsync("sendfrom", account, address, amount == decimal.MinusOne ? decimal.Parse(await TippingManager.InvokeMethodAsync("getbalance", account).ConfigureAwait(false)) : amount).ConfigureAwait(false);
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("出金完了")
                        .WithDescription($"```{address}```\nに出金しました。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Green)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .AddInlineField("金額", $"{amount:N8} ZNY")
                        .AddInlineField("トランザクションID", txid)
            ).ConfigureAwait(false);
        }
    }
}
