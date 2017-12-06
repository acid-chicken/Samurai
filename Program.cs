using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace AcidChicken.Samurai
{
    using Models;

    public static class Program
    {
        public const string ConfigurePath = "config.json";

        public static Config Config { get; set; }

        public static DiscordSocketClient DiscordClient { get; set; }

        public static DiscordSocketConfig DiscordClientConfig { get; set; }

        public static HttpClient HttpClient { get; set; } = new HttpClient();

        public static async Task Main(string[] args)
        {
            if (File.Exists(ConfigurePath))
            {
                Config = await LoadConfigAsync().ConfigureAwait(false);
            }
            else
            {
                await SaveConfigAsync(new Config()).ConfigureAwait(false);
                return;
            }

            DiscordClientConfig = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose
            };
            DiscordClient = new DiscordSocketClient(DiscordClientConfig);
            DiscordClient.Log += LogAsync;

            await DiscordClient.LoginAsync(TokenType.Bot, Config.DiscordToken).ConfigureAwait(false);
            await DiscordClient.StartAsync().ConfigureAwait(false);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        public static async Task<Config> LoadConfigAsync(string path = ConfigurePath)
        {
            using (var stream = File.OpenRead(path))
            using (var reader = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<Config>(await reader.ReadToEndAsync().ConfigureAwait(false));
            }
        }

        public static async Task SaveConfigAsync(Config config, string path = ConfigurePath)
        {
            using (var stream = File.OpenWrite(path))
            using (var writer = new StreamWriter(stream))
            {
                await writer.WriteLineAsync(JsonConvert.SerializeObject(config, Formatting.Indented)).ConfigureAwait(false);
            }
        }

        public static async Task LogAsync(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogSeverity.Verbose:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
            await Console.Out.WriteLineAsync($"[{message.Source}]{message.Message}");
            Console.ResetColor();
        }
    }
}
