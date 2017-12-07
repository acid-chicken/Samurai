using System;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Models
{
    using Tasks;

    [JsonObject]
    public class Ticker
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("price_usd")]
        public string PriceUsd { get; set; }

        [JsonProperty("price_btc")]
        public string PriceBtc { get; set; }

        [JsonProperty("24h_volume_usd")]
        public string TwentyfourhoursVolumeUsd { get; set; }

        [JsonProperty("market_cap_usd")]
        public string MarketCapUsd { get; set; }

        [JsonProperty("avaliable_supply")]
        public string AvaliableSupply { get; set; }

        [JsonProperty("total_supply")]
        public string TotalSupply { get; set; }

        [JsonProperty("max_supply")]
        public string MaxSupply { get; set; }

        [JsonProperty("percent_change_1h")]
        public string PercentChangeOnehour { get; set; }

        [JsonProperty("percent_change_24h")]
        public string PercentChangeTwentyfourhours { get; set; }

        [JsonProperty("percent_change_7d")]
        public string PercentChangeSevenDays { get; set; }

        [JsonProperty("last_updated")]
        public string LastUpdated { get; set; }

        [JsonProperty("price_jpy")]
        public string PriceJpy { get; set; }

        [JsonProperty("24h_volume_jpy")]
        public string TwentyfourhoursVolumeJpy { get; set; }

        [JsonProperty("market_cap_jpy")]
        public string MarketCapJpy { get; set; }
    }
}
