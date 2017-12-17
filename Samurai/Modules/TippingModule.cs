using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;

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
            var balance = await TippingManager.InvokeMethodAsync("getbalance", account, 0).ConfigureAwait(false);
            var queued = TippingManager.Queue.Where(x => x.From == Context.User.Id).Sum(x => x.Amount);
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
                        .AddInlineField("利用可能", $"{decimal.Parse(balance) - queued:N8} ZNY")
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

        [Command("rain"), Summary("指定した期間以降に発言したユーザー全員に均等に投げ銭します。"), Alias("撒金"), RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task RainAsync([Summary("金額")] decimal totalAmount, [Summary("対象期間(時間単位)")] double hours = 24.0)
        {
            var targets = new HashSet<IUser>();
            switch (Context.Channel)
            {
                case IGuildChannel guild:
                {
                    var channels = new List<IMessage>();
                    await Task.WhenAll((await guild.Guild.GetTextChannelsAsync().ConfigureAwait(false)).Select(async channel =>
                    {
                        try
                        {
                            var messages = new List<IMessage>(await channel.GetMessagesAsync().Flatten().ConfigureAwait(false)).OrderByDescending(x => x.CreatedAt).ToList();
                            while (Context.Message.CreatedAt - messages.LastOrDefault().CreatedAt <= TimeSpan.FromHours(hours))
                            {
                                await channel.GetMessagesAsync(messages.LastOrDefault(), Direction.Before).ForEachAsync(x => messages.AddRange(x)).ConfigureAwait(false);
                            }
                            channels.AddRange(messages);
                            messages.Select(x => x.Author).Select(x => targets.Add(x));
                        }
                        catch (HttpException ex) when ((ex.DiscordCode ?? 0) == 50001) { }
                    })).ConfigureAwait(false);
                    break;
                }
                case IGroupChannel group:
                {
                    var messages = new List<IMessage>(await group.GetMessagesAsync().Flatten().ConfigureAwait(false)).OrderByDescending(x => x.CreatedAt).ToList();
                    while (Context.Message.CreatedAt - messages.LastOrDefault().CreatedAt <= TimeSpan.FromHours(hours))
                    {
                        await group.GetMessagesAsync(messages.LastOrDefault(), Direction.Before).ForEachAsync(x => messages.AddRange(x)).ConfigureAwait(false);
                    }
                    targets = messages.Select(x => x.Author).Distinct().ToHashSet();
                    break;
                }
            }
            targets.Remove(Context.User);
            targets.RemoveWhere(x => string.IsNullOrEmpty(x.GetAvatarUrl()));
            var limit = DateTimeOffset.Now.AddDays(3);
            var amount = totalAmount / targets.Count;
            var mentions = string.Join(' ', targets.Select(x => x.Mention));
            var isExtract = mentions.Length > EmbedBuilder.MaxDescriptionLength;
            if (isExtract)
            {
                var chars = mentions.Take(EmbedBuilder.MaxDescriptionLength);
                mentions = new string(chars.Take(chars.ToList().LastIndexOf('>')).ToArray());
            }
            await Task.WhenAll(targets.Select(x => TippingManager.EnqueueAsync(new Models.TipQueue(Context.User.Id, x.Id, limit, amount))).Append(ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("撒き銭完了")
                        .WithDescription($"撒き銭しました。DM通知は行われませんのでご注意下さい。")
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Green)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .AddInlineField("一人あたりの金額", $"{amount:N8} ZNY")
                        .AddInlineField("対象者数", $"{targets.Count} 人")
                        .AddInlineField("総金額", $"{totalAmount:N8} ZNY")
                        .AddInlineField(isExtract ? "対象者（抜粋）" : "対象者", mentions)
            ))).ConfigureAwait(false);
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
