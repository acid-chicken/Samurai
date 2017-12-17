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

        [Command("rain"), Summary("条件を満たしたユーザー全員に均等に投げ銭します。"), Alias("撒金"), RequireContext(ContextType.Guild | ContextType.Group)]
        public async Task RainAsync([Summary("金額")] decimal totalAmount)
        {
            var targets =
                JsonConvert
                    .DeserializeObject<Dictionary<string, decimal>>("{\"\":0.00000000,\"discord:126588835298672641\":0.00000000,\"discord:129163732667465728\":11.00000000,\"discord:133820962582822912\":9.49990000,\"discord:133884625599725568\":0.00000000,\"discord:190137850698792961\":3.00000000,\"discord:208588471579705345\":0.00000000,\"discord:210711179289559051\":0.00000000,\"discord:218757612932562946\":19.80000000,\"discord:220506362197704704\":1.14514000,\"discord:220543069378969601\":180.00000000,\"discord:225876235560026122\":0.00000000,\"discord:225935114675159040\":0.00000000,\"discord:226127482754039810\":0.00000000,\"discord:234467182753349632\":0.00000000,\"discord:238882362379730945\":0.00000000,\"discord:243394719336366081\":0.00000000,\"discord:246516021345648652\":0.00000000,\"discord:246834514146361345\":0.00000000,\"discord:249862566979829761\":10.00000000,\"discord:249871363446276106\":0.00000000,\"discord:255342410010329089\":0.00000000,\"discord:257475501675905024\":0.00000000,\"discord:257492069021384704\":11.00000000,\"discord:258962965997420544\":50.53805819,\"discord:258966923289690112\":0.00000000,\"discord:281075009894744064\":0.00000000,\"discord:294841976732254208\":0.00000000,\"discord:303541679279833090\":20.00000000,\"discord:310413442760572929\":0.00000000,\"discord:313250757086281741\":0.00000000,\"discord:320524874328309761\":10.00000000,\"discord:321308607163400192\":0.00000000,\"discord:321312526937751553\":0.00000000,\"discord:325780546963636226\":10.84295000,\"discord:336516260290363392\":0.00000000,\"discord:341027720181841920\":0.00000000,\"discord:342319481797738498\":1.00000000,\"discord:360952167445692437\":0.00000000,\"discord:362635781443158016\":0.00000000,\"discord:368273610709925901\":0.00000000,\"discord:371113715258490882\":0.00000000,\"discord:372312504250138624\":0.00000000,\"discord:374524675428188160\":0.00000000,\"discord:375517123146940426\":0.00000000,\"discord:377845897603842049\":0.00000000,\"discord:378564233941614592\":0.00000000,\"discord:381373135036874753\":0.00000000,\"discord:382839015679721473\":0.00000000,\"discord:382952555950637058\":0.00000000,\"discord:384728108923617283\":0.00000000,\"discord:385198798751793166\":0.00000000,\"discord:385792379359068160\":0.00000000,\"discord:386892987881357312\":0.00000000,\"discord:387944196272816128\":0.00000000,\"discord:388511320271618050\":1.00000000,\"discord:388723141641502732\":0.00000000,\"discord:388974332216344586\":0.00000000,\"discord:389086035885293568\":0.00000000,\"discord:389087753184477188\":0.00000000,\"discord:389662074340376577\":0.00000000,\"discord:389689324544851978\":0.00000000,\"discord:389965021335257088\":18.81450000,\"discord:389988242138857499\":0.00000000,\"discord:390082844158066698\":0.00000000,\"discord:390122912083869696\":0.00000000,\"discord:390141488132128769\":0.00000000,\"discord:390149134650310656\":10.01000000,\"discord:390158081717436426\":0.00000000,\"discord:390161207828545546\":0.00000000,\"discord:390176767568117761\":6.49990000,\"discord:390211226480672782\":0.00000000,\"discord:390243861194080267\":11.00000000,\"discord:390308414397612033\":0.00000000,\"discord:390319845884952587\":0.00000000,\"discord:390325030250348555\":0.00000000,\"discord:390340274913804288\":0.00000000,\"discord:390354736999825408\":0.00000000,\"discord:390374995148144653\":0.00000000,\"discord:390378074371391499\":0.00000000,\"discord:390673978865352705\":0.00000000,\"discord:390694702422163457\":0.00000000,\"discord:391031975441596416\":0.00000000,\"discord:391423056024698880\":0.00000000,\"discord:391579093927329793\":0.00000000,\"test\":0.00000000}")
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
                            .WithDescription("撒き銭しました。DM通知は行われませんのでご注意下さい。")
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
