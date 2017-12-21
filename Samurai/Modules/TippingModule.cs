using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using LiteDB;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Modules
{
    using static Program;
    using Assets;
    using Components;
    using Models;
    using Tasks;

    [Group(""), Summary("投げ銭モジュールです。")]
    public class TippingModule : ModuleBase
    {
        [Command("balance"), Summary("残高を表示します。"), Alias("残高")]
        public async Task BalanceAsync([Remainder] string comment = null)
        {
            var account = TippingManager.GetAccountName(Context.User);
            var address = await TippingManager.EnsureAccountAsync(account).ConfigureAwait(false);
            var earned = decimal.Zero;
            await Task.WhenAll(TippingManager.GetCollection().Find(x => x.To == Context.User.Id).Select(async x =>
            {
                try
                {
                    earned += x.Amount;
                    var from = DiscordClient.GetUser(x.From);
                    var txid = await TippingManager.InvokeMethodAsync("sendfrom", TippingManager.GetAccountName(from), address, x.Amount).ConfigureAwait(false);
                    var isDeleted = await TippingManager.DeleteRequestAsync(x.Id).ConfigureAwait(false);
                    await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingModule", $"Sent {x.Amount} ZNY from {from.Username}#{from.Discriminator} to {Context.User.Username}#{Context.User.Discriminator}."));
                }
                catch (Exception ex)
                {
                    await RequestLogAsync(new LogMessage(LogSeverity.Error, "TippingModule", ex.Message, ex));
                }
            }));
            var balance0 = decimal.Parse(await TippingManager.InvokeMethodAsync("getbalance", account, 0).ConfigureAwait(false));
            var balance1 = decimal.Parse(await TippingManager.InvokeMethodAsync("getbalance", account, 1).ConfigureAwait(false));
            var queued = TippingManager.GetCollection().Find(x => x.From == Context.User.Id).Sum(x => x.Amount);
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
                        .AddInlineField("利用可能", $"{balance1 - queued:N8} ZNY")
                        .AddInlineField("検証待ち", $"{balance0 - balance1:N8} ZNY")
                        .AddInlineField("受取待ち", $"{queued:N8} ZNY")
                        .AddInlineField("受け取り", $"{earned:N8} ZNY")
            ).ConfigureAwait(false);
        }

        [Command("deposit"), Summary("入金用アドレスを表示します。")]
        public async Task DepositAsync([Remainder] string comment = null)
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
            if (TippingManager.GetIsAndroidMode(Context.User))
            {
                await ReplyAsync($"```{address}```");
            }
        }

        [Command("rain"), Summary("条件を満たしたユーザー全員に均等に投げ銭します。端数で総金額が多少変動することがあります。末尾に`powerful`をつけると、総金額ではなく一人あたりに投げ銭される金額を指定したことになります。"), Alias("撒金"), RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task RainAsync([Summary("金額")] decimal totalAmount = decimal.MinusOne, [Remainder] string comment = null)
        {
            if (totalAmount == decimal.MinusOne)
            {
                totalAmount = (decimal)Math.Pow(10, Program.Random.NextDouble()) - 1;
            }
            var targets = await TippingManager.GetUsersAsync(Context.Channel, Context.User, 10).ConfigureAwait(false);
            if (targets.Any())
            {
                var limit = DateTimeOffset.Now.AddDays(3);
                var amount = comment?.ToLower()?.Contains("powerful") ?? false ? totalAmount : Math.Truncate(totalAmount / targets.Count * 10000000) / 10000000;
                var count = targets.Count;
                var embed =
                    new EmbedBuilder()
                        .WithTitle("撒き銭完了")
                        .WithDescription("撒き銭しました。DM通知は行われませんのでご注意下さい。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Green)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .AddInlineField("一人あたりの金額", $"{amount:N8} ZNY")
                        .AddInlineField("対象者数", $"{count} 人")
                        .AddInlineField("総金額", $"{amount * count:N8} ZNY");
                if (!string.IsNullOrEmpty(comment))
                {
                    embed = embed.AddField("コメント", comment);
                }
                await Task.WhenAll(targets.Select(x => TippingManager.AddRequestAsync(new TipRequest(Context.User.Id, x.Id, amount, limit))).Append(ReplyAsync
                (
                    message: Context.User.Mention,
                    embed: embed
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
                            .WithDescription("撒き銭に失敗しました。撒き銭対象となれるユーザーがいないか、指定した金額が不正である可能性があります。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
        }

        [Command("send"), Summary("指定されたユーザーに送金します。"), Alias("送金")]
        public async Task SendAsync([Summary("送り先のユーザー")] IUser user, [Summary("金額")] decimal amount, [Remainder] string comment = null)
        {
            var account = TippingManager.GetAccountName(Context.User);
            var address = await TippingManager.EnsureAccountAsync(TippingManager.GetAccountName(user)).ConfigureAwait(false);
            var balance1 = decimal.Parse(await TippingManager.InvokeMethodAsync("getbalance", account, 1).ConfigureAwait(false));
            var queued = TippingManager.GetCollection().Find(x => x.From == Context.User.Id).Sum(x => x.Amount);
            if (amount > 0 && amount < balance1 - queued)
            {
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
                            .AddInlineField("トランザクションID", $"```{txid}```")
                ).ConfigureAwait(false);
                if (TippingManager.GetIsAndroidMode(Context.User))
                {
                    await ReplyAsync($"```{txid}```");
                }
                var embed =
                    new EmbedBuilder()
                        .WithTitle("送金通知")
                        .WithDescription($"{Context.User.Mention} からの送金を受け取りました。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Orange)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(user)
                        .WithThumbnailUrl(Context.User.GetAvatarUrl())
                        .AddInlineField("金額", $"{amount:N8} ZNY")
                        .AddInlineField("トランザクションID", $"```{txid}```");
                if (!string.IsNullOrEmpty(comment))
                {
                    embed = embed.AddField("コメント", comment);
                }
                var dm = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
                await dm.SendMessageAsync
                (
                    text: user.Mention,
                    embed: embed
                ).ConfigureAwait(false);
                if (TippingManager.GetIsAndroidMode(user))
                {
                    await dm.SendMessageAsync($"```{txid}```");
                }
            }
            else
            {
                await ReplyAsync
                (
                    message: Context.User.Mention,
                    embed:
                        new EmbedBuilder()
                            .WithTitle("送金失敗")
                            .WithDescription("送金に失敗しました。残高が不足している可能性があります。")
                            .WithCurrentTimestamp()
                            .WithColor(Colors.Red)
                            .WithFooter(EmbedManager.CurrentFooter)
                            .WithAuthor(Context.User)
                ).ConfigureAwait(false);
            }
            await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingModule", $"Sent {amount:N8} ZNY from {Context.User.Username}#{Context.User.Discriminator} to {user.Username}#{user.Discriminator}.")).ConfigureAwait(false);
        }

        [Command("tip"), Summary("指定されたユーザーに投げ銭します。"), Alias("投銭")]
        public async Task TipAsync([Summary("送り先のユーザー")] IUser user, [Summary("金額")] decimal amount, [Remainder] string comment = null)
        {
            var limit = DateTimeOffset.Now.AddDays(3);
            await TippingManager.AddRequestAsync(new TipRequest(Context.User.Id, user.Id, amount, limit));
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
            ).ConfigureAwait(false);
            var embed =
                new EmbedBuilder()
                    .WithTitle("投げ銭通知")
                    .WithDescription($"{Context.User.Mention} から投げ銭が届いています。受取期限までに`{ModuleManager.Prefix}balance`を実行することで投げ銭を受け取れます。")
                    .WithCurrentTimestamp()
                    .WithColor(Colors.Orange)
                    .WithFooter(EmbedManager.CurrentFooter)
                    .WithAuthor(user)
                    .WithThumbnailUrl(Context.User.GetAvatarUrl())
                    .AddInlineField("金額", $"{amount:N8} ZNY")
                    .AddInlineField("受取期限", limit);
            if (!string.IsNullOrEmpty(comment))
            {
                embed = embed.AddField("コメント", comment);
            }
            var dm = await user.GetOrCreateDMChannelAsync().ConfigureAwait(false);
            await dm.SendMessageAsync
            (
                text: user.Mention,
                embed: embed
            ).ConfigureAwait(false);
        }

        [Command("withdraw"), Summary("指定されたアドレスに出金します。金額を指定しなかった場合は全額出金されます。"), Alias("出金")]
        public async Task WithDrawAsync([Summary("送り先のアドレス")] string address, [Summary("金額")] decimal amount = decimal.MinusOne, [Remainder] string comment = null)
        {
            var account = TippingManager.GetAccountName(Context.User);
            await TippingManager.EnsureAccountAsync(account).ConfigureAwait(false);
            var balance1 = decimal.Parse(await TippingManager.InvokeMethodAsync("getbalance", account, 1).ConfigureAwait(false));
            var queued = TippingManager.GetCollection().Find(x => x.From == Context.User.Id).Sum(x => x.Amount);
            if (amount > 0 && amount < balance1 - queued)
            {
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
                            .AddInlineField("トランザクションID", $"```{txid}```")
                ).ConfigureAwait(false);
                if (TippingManager.GetIsAndroidMode(Context.User))
                {
                    await ReplyAsync($"```{txid}```");
                }
            }
        }
    }
}
