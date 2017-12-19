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
using LiteDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AcidChicken.Samurai.Tasks
{
    using static Program;
    using Components;
    using Models;

    public static class TippingManager
    {
        public static Task AddRequestAsync(TipRequest request)
        {
            var collection = GetCollection();
            collection.Insert(request);
            collection.EnsureIndex(x => x.From);
            collection.EnsureIndex(x => x.To);
            return Task.CompletedTask;
        }

        public static Task<bool> DeleteRequestAsync(BsonValue value) => Task.FromResult(GetCollection().Delete(value));

        public static Task<string> EnsureAccountAsync(string account) => InvokeMethodAsync("getaccountaddress", account);

        public static string GetAccountName(IUser user) => GetAccountName(user.Id);

        public static string GetAccountName(ulong id) => $"discord:{id}";

        public static LiteCollection<TipRequest> GetCollection() => Database.GetCollection<TipRequest>("tiprequests");

        public static async Task<HashSet<IUser>> GetUsersAsync(IChannel channel, IUser exclude, decimal credit = decimal.Zero) =>
            JsonConvert
                .DeserializeObject<Dictionary<string, decimal>>(await InvokeMethodAsync("listaccounts").ConfigureAwait(false))
                .Where
                (user =>
                    user.Key.StartsWith("discord:") &&
                    user.Value >= 10 &&
                    channel
                        .GetUsersAsync()
                        .Flatten()
                        .Result
                            .Where(x => x.Id != exclude.Id)
                            .Select(x => $"discord:{x.Id}")
                            .Contains(user.Key)
                )
                .Select(x => (IUser)DiscordClient.GetUser(ulong.TryParse(new string(x.Key.Skip(8).ToArray()), out ulong result) ? result : 0))
                .ToHashSet();

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
            while (!token.IsCancellationRequested)
            {
                await Task.WhenAll
                (
                    RequestLogAsync(new LogMessage(LogSeverity.Verbose, "TippingManager", "Calling tasks.")),
                    CheckRequestsAsync(),
                    Task.Delay(60000)
                ).ConfigureAwait(false);
            }
        }

        public static Task CheckRequestsAsync()
        {
            GetCollection().Delete(x => x.Limit < DateTimeOffset.Now);
            return Task.CompletedTask;
        }
    }
}
