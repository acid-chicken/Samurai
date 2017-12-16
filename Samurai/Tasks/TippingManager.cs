using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EdjCase.JsonRpc.Client;
using EdjCase.JsonRpc.Core;
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

        public static Task<string> EnsureAccountAsync(string account) => TippingManager.InvokeMethodAsync("getaccountaddress", account);

        public static string GetAccountName(IUser user) => $"discord:{user.Id}";

        public static async Task<string> InvokeMethodAsync(string method, params object[] args) => (await RpcClient.SendRequestAsync(new RpcRequest(new RpcId("request"), method, JToken.Parse($"[{string.Join(',', args.Select(x => x == null || x is bool || x is sbyte || x is byte || x is short || x is ushort || x is int || x is uint || x is long || x is ulong || x is float || x is double || x is decimal ? x.ToString() : $"\"{x}\""))}]"))).ConfigureAwait(false)).GetResult<string>();

        public static Task WorkAsync() => WorkAsync(default);

        public static async Task WorkAsync(CancellationToken token = default)
        {
            Queue = Queue.Union(ApplicationConfig.Queue).ToList();
            RpcClient = new RpcClient
            (
                baseUrl: new Uri(ApplicationConfig.RpcServer),
                authHeaderValue: new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ApplicationConfig.RpcUser}:{ApplicationConfig.RpcPassword}"))),
                contentType: "application/json-rpc"
            );
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
