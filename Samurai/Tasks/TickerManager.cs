using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Tasks
{
    using Models;

    public static class TickerManager
    {
        public static Ticker Ticker { get; set; }

        public static Task WorkAsync() => WorkAsync(default);

        public static async Task WorkAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll
                (
                    SetGameAsTickerAsync(),
                    Task.Delay(60000)
                ).ConfigureAwait(false);
            }
        }

        public static async Task SetGameAsTickerAsync()
        {
            try
            {
                using (var request = await Program.HttpClient.GetAsync("https://api.coinmarketcap.com/v1/ticker/bitzeny/?convert=JPY").ConfigureAwait(false))
                using (var response = request.Content)
                {
                    Ticker = JsonConvert.DeserializeObject<Ticker[]>(await response.ReadAsStringAsync().ConfigureAwait(false))[0];
                    var game = $"[{DateTimeOffset.FromUnixTimeSeconds(long.TryParse(Ticker.LastUpdated ?? "0", out long x) ? x : 0).ToLocalTime():M/d HH:mm}] {(double.TryParse(Ticker.PriceJpy ?? "0", out double y) ? y : 0):N3} JPY (hourly: {Ticker.PercentChangeOnehour}% / daily: {Ticker.PercentChangeTwentyfourhours}% / weekly: {Ticker.PercentChangeSevenDays}%)";
                    await Program.DiscordClient.SetGameAsync(game).ConfigureAwait(false);
                    await Program.LogAsync(new LogMessage(LogSeverity.Verbose, "TickerManager", $"Set game to \"{game}\".")).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await Program.LogAsync(new LogMessage(LogSeverity.Error, "TickerManager", ex.Message, ex)).ConfigureAwait(false);
            }
        }
    }
}
