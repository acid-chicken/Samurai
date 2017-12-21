using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AcidChicken.Samurai.Components
{
    using static Program;
    using Models;
    public static class ApiManager
    {
        public static async Task<Ticker> GetTickerAsync(string id, string convert = null)
        {
            using (var request = await Program.HttpClient.GetAsync($"https://api.coinmarketcap.com/v1/ticker/{id}/{(string.IsNullOrEmpty(convert) ? "" : $"?convert={convert}")}").ConfigureAwait(false))
            using (var response = request.Content)
            {
                return JsonConvert.DeserializeObject<Ticker[]>(await response.ReadAsStringAsync().ConfigureAwait(false))[0];
            }
        }

        public static async Task<Dictionary<string, string>> GetTickerAsDictionaryAsync(string id, string convert = null)
        {
            using (var request = await Program.HttpClient.GetAsync($"https://api.coinmarketcap.com/v1/ticker/{id}/{(string.IsNullOrEmpty(convert) ? "" : $"?convert={convert}")}").ConfigureAwait(false))
            using (var response = request.Content)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>[]>(await response.ReadAsStringAsync().ConfigureAwait(false))[0];
            }
        }

        public static async Task<Ticker[]> GetTickersAsync(string convert = null)
        {
            using (var request = await Program.HttpClient.GetAsync($"https://api.coinmarketcap.com/v1/ticker/?limit=0{(string.IsNullOrEmpty(convert) ? "" : $"&convert={convert}")}").ConfigureAwait(false))
            using (var response = request.Content)
            {
                return JsonConvert.DeserializeObject<Ticker[]>(await response.ReadAsStringAsync().ConfigureAwait(false));
            }
        }

        public static async Task<Dictionary<string, string>[]> GetTickersAsDictionaryAsync(string convert = null)
        {
            using (var request = await Program.HttpClient.GetAsync($"https://api.coinmarketcap.com/v1/ticker/?limit=0{(string.IsNullOrEmpty(convert) ? "" : $"&convert={convert}")}").ConfigureAwait(false))
            using (var response = request.Content)
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>[]>(await response.ReadAsStringAsync().ConfigureAwait(false));
            }
        }
    }
}
