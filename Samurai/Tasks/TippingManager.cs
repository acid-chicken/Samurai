using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AcidChicken.Samurai.Tasks
{
    using static Program;
    using Models;

    public static class TippingManager
    {
        public static List<TipQueue> Queue { get; set; } = new List<TipQueue>();

        public static async Task<bool> DequeueAsync(TipQueue queue)
        {
            var result = Queue.Remove(queue);
            await SaveConfigAsync(ApplicationConfig).ConfigureAwait(false);
            return result;
        }

        public static async Task EnqueueAsync(TipQueue queue)
        {
            Queue.Add(queue);
            await SaveConfigAsync(ApplicationConfig).ConfigureAwait(false);
        }

        public static async Task<string> EnsureAccountAsync(string account) => (await TippingManager.InvokeMethodAsync<string>("getaccountaddress", account).ConfigureAwait(false)).Result;

        public static string GetAccountName(IUser user) => $"discord:{user.Id}";

        public static async Task<RpcResponse<T>> InvokeMethodAsync<T>(string method, params object[] args)
        {
            using (var response = await BitZenyClient.PostAsync("", new StringContent($"{{\"jsonrpc\":\"2.0\",\"id\":\"1\",\"method\":{method},\"params\":[{string.Join(',', args.Select(x => x == null || x is bool || x is sbyte || x is byte || x is short || x is ushort || x is int || x is uint || x is long || x is ulong || x is float || x is double || x is decimal ? x.ToString() : $"\"{x}\""))}]", Encoding.UTF8, "application/json-rpc")).ConfigureAwait(false))
            using (var content = response.Content)
            {
                return JsonConvert.DeserializeObject<RpcResponse<T>>(await content.ReadAsStringAsync().ConfigureAwait(false));
            }
        }

        public static Task WorkAsync() => WorkAsync(default);

        public static async Task WorkAsync(CancellationToken token = default)
        {
            Queue = Queue.Union(ApplicationConfig.Queue).ToList();
            BitZenyClient = new HttpClient()
            {
                BaseAddress = new Uri(ApplicationConfig.RpcServer),
                Timeout = Timeout.InfiniteTimeSpan
            };
            BitZenyClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ApplicationConfig.RpcUser}:{ApplicationConfig.RpcPassword}")));

            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll
                (
                    RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingManager", "Calling tasks.")),
                    CheckQueueAsync(),
                    Task.Delay(60000)
                ).ConfigureAwait(false);
            }
        }

        public static Task CheckQueueAsync()
        {
            Queue.RemoveAll(x => x.Limit < DateTimeOffset.Now);
            return Task.CompletedTask;
        }
    }
}
