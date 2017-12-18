using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static Dictionary<ulong, List<TipQueue>> Queue { get; set; } = new Dictionary<ulong, List<TipQueue>>();

        public static Task<bool> DequeueAsync(ulong id, TipQueue queue)
        {
            Queue[id].RemoveAll(x => x == null);
            return Task.FromResult(Queue[id].Remove(queue));
        }

        public static Task EnqueueAsync(ulong id, TipQueue queue)
        {
            Queue[id].Add(queue);
            Queue[id].RemoveAll(x => x == null);
            return Task.CompletedTask;
        }

        public static Task<string> EnsureAccountAsync(string account) => InvokeMethodAsync("getaccountaddress", account);

        public static string GetAccountName(IUser user) => $"discord:{user.Id}";

        public static async Task<string> InvokeMethodAsync(params object[] args)
        {
            using (var process = Process.Start(new ProcessStartInfo("bitzeny-cli", string.Join(' ', args.Select(x => x == null || x is bool || x is sbyte || x is byte || x is short || x is ushort || x is int || x is uint || x is long || x is ulong || x is float || x is double || x is decimal ? x.ToString() : $"\"{x}\"")))
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }))
            using (var reader = process.StandardOutput)
            {
                var output = await reader.ReadToEndAsync().ConfigureAwait(false);
                process.WaitForExit();
                process.Close();
                return output.Trim();
            }
        }

        public static Task WorkAsync() => WorkAsync(default);

        public static async Task WorkAsync(CancellationToken token = default)
        {
            Queue = Queue.ToList().Union(ApplicationConfig.Queue).ToDictionary(x => x.Key, x => x.Value);
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
            Queue.Select(user => user.Value.RemoveAll(x => x == null || x.Limit < DateTimeOffset.Now));
            return Task.CompletedTask;
        }
    }
}
