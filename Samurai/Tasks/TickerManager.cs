using System;
using System.Threading;
using System.Threading.Tasks;
using AcidChicken.Samurai.Components;
using AcidChicken.Samurai.Models;
using Discord;

namespace AcidChicken.Samurai.Tasks
{
    using static Program;

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
                    RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TickerManager", "Calling tasks.")),
                    SetGameAsTickerAsync(),
                    Task.Delay(60000)
                ).ConfigureAwait(false);
            }
        }

        public static async Task SetGameAsTickerAsync()
        {
            try
            {
                Ticker = await ApiManager.GetTickerAsync("bitzeny", "JPY").ConfigureAwait(false);
                var game = $"[{DateTimeOffset.FromUnixTimeSeconds(long.TryParse(Ticker.LastUpdated ?? "0", out long x) ? x : 0).ToLocalTime():M/d HH:mm}] {(double.TryParse(Ticker.PriceJpy ?? "0", out double y) ? y : 0):N3} JPY (hourly: {(Ticker.PercentChangeOnehour.StartsWith('-') || Ticker.PercentChangeOnehour == "0.00" ? "" : "+")}{Ticker.PercentChangeOnehour}% / daily: {(Ticker.PercentChangeTwentyfourhours.StartsWith('-') || Ticker.PercentChangeTwentyfourhours == "0.00" ? "" : "+")}{Ticker.PercentChangeTwentyfourhours}% / weekly: {(Ticker.PercentChangeSevenDays.StartsWith('-') || Ticker.PercentChangeSevenDays == "0.00" ? "" : "+")}{Ticker.PercentChangeSevenDays}%)";
                await Task.WhenAll
                (
                    DiscordClient.SetGameAsync(game),
                    RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TickerManager", $"Set game to \"{game}\"."))
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await RequestLogAsync(new LogMessage(LogSeverity.Error, "TickerManager", ex.Message, ex)).ConfigureAwait(false);
            }
        }
    }
}
