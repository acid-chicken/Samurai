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

    [Group("conversion"), Summary("通貨換算モジュールです。"), Alias("conv", "c")]
    public class ConversionModule : ModuleBase
    {
        public static string UnitedStatesDollar = "USD";
        public static string[] Convertable = new [] { "AUD", "BRL", "CAD", "CHF", "CLP", "CNY", "CZK", "DKK", "EUR", "GBP", "HKD", "HUF", "IDR", "ILS", "INR", "JPY", "KRW", "MXN", "MYR", "NOK", "NZD", "PHP", "PKR", "PLN", "RUB", "SEK", "SGD", "THB", "TRY", "TWD", "ZAR" };

        [Command(""), Summary("現在のレートで通貨を換算します。")]
        public async Task ConvertAsync([Summary("換算元の通貨")] string before, [Summary("換算先の通貨")] string after, [Summary("換算額")] decimal volume)
        {
            var result = volume;
            var route = string.Empty;
            var afterL = after.ToLower();
            var beforeL = before.ToLower();
            var afterU = after.ToUpper();
            var beforeU = before.ToUpper();
            var isAfterUSD = afterU == UnitedStatesDollar;
            var isAfterC = Convertable.Contains(afterU);
            var isBeforeUSD = beforeU == UnitedStatesDollar;
            var isBeforeC = Convertable.Contains(beforeU);
            if (isAfterUSD)
            {
                if (isBeforeUSD)
                {
                    route = "USD > USD";
                }
                else if (isBeforeC)
                {
                    var beforeD = await ApiManager.GetTickerAsDictionaryAsync("bitcoin", beforeU).ConfigureAwait(false);
                    result *= decimal.Parse(beforeD[$"price_usd"]) / decimal.Parse(beforeD[$"price_{beforeL}"]);
                    route = $"{beforeU} > BTC > USD";
                }
                else
                {
                    var afterDs = await ApiManager.GetTickersAsDictionaryAsync().ConfigureAwait(false);
                    var afterBD = afterDs.First(x => x["id"] == beforeL || x["name"] == before || x["symbol"] == beforeU);
                    result *= decimal.Parse(afterBD[$"price_usd"]);
                    route = $"{beforeU} > USD";
                }
            }
            else if (isAfterC)
            {
                if (isBeforeUSD)
                {
                    var afterD = await ApiManager.GetTickerAsDictionaryAsync("bitcoin", afterU).ConfigureAwait(false);
                    result *= decimal.Parse(afterD[$"price_{afterL}"]) / decimal.Parse(afterD[$"price_usd"]);
                    route = $"USD > BTC > {afterU}";
                }
                else if (isBeforeC)
                {
                    var afterD = await ApiManager.GetTickerAsDictionaryAsync("bitcoin", afterU).ConfigureAwait(false);
                    var beforeD = await ApiManager.GetTickerAsDictionaryAsync("bitcoin", beforeU).ConfigureAwait(false);
                    result *= decimal.Parse(afterD[$"price_{afterL}"]) / decimal.Parse(beforeD[$"price_{beforeL}"]);
                    route = $"{beforeU} > BTC > {afterU}";
                }
                else
                {
                    var afterDs = await ApiManager.GetTickersAsDictionaryAsync(afterU).ConfigureAwait(false);
                    var afterBD = afterDs.First(x => x["id"] == beforeL || x["name"] == before || x["symbol"] == beforeU);
                    result *= decimal.Parse(afterBD[$"price_{afterL}"]);
                    route = $"{beforeU} > {afterU}";
                }
            }
            else
            {
                if (isBeforeUSD)
                {
                    var beforeDs = await ApiManager.GetTickersAsDictionaryAsync().ConfigureAwait(false);
                    var beforeBD = beforeDs.First(x => x["id"] == afterL || x["name"] == after || x["symbol"] == afterU);
                    result /= decimal.Parse(beforeBD[$"price_usd"]);
                    route = $"USD > {afterU}";
                }
                else if (isBeforeC)
                {
                    var beforeDs = await ApiManager.GetTickersAsDictionaryAsync(beforeU).ConfigureAwait(false);
                    var beforeBD = beforeDs.First(x => x["id"] == afterL || x["name"] == after || x["symbol"] == afterU);
                    result /= decimal.Parse(beforeBD[$"price_{beforeL}"]);
                    route = $"{beforeU} > {afterU}";
                }
                else
                {
                    var afterDs = await ApiManager.GetTickersAsDictionaryAsync().ConfigureAwait(false);
                    var beforeDs = await ApiManager.GetTickersAsDictionaryAsync().ConfigureAwait(false);
                    var afterBD = afterDs.First(x => x["id"] == beforeL || x["name"] == before || x["symbol"] == beforeU);
                    var beforeBD = beforeDs.First(x => x["id"] == afterL || x["name"] == after || x["symbol"] == afterU);
                    result *= decimal.Parse(afterBD["price_usd"]) / decimal.Parse(beforeBD["price_usd"]);
                    route = $"{beforeU} > BTC > {afterU}";
                }
            }
            await ReplyAsync
            (
                message: Context.User.Mention,
                embed:
                    new EmbedBuilder()
                        .WithTitle("換算結果")
                        .WithDescription(route)
                        .WithCurrentTimestamp()
                        .WithColor(Colors.Blue)
                        .WithFooter(EmbedManager.CurrentFooter)
                        .WithAuthor(Context.User)
                        .AddInlineField("換算前", $"{volume} {before}")
                        .AddInlineField("換算後", $"{result} {after}")
            ).ConfigureAwait(false);
        }
    }
}
