using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace AcidChicken.Samurai
{
    using Components;
    using Models;
    using Modules;
    using Tasks;

    public static class Program
    {
        public const string ConfigurePath = "config.json";

        public static Config ApplicationConfig { get; set; }

        public static DiscordSocketClient DiscordClient { get; set; }

        public static DiscordSocketConfig DiscordClientConfig { get; set; }

        public static HttpClient HttpClient { get; set; } = new HttpClient();

        public static bool IsLoggerLocked { get; private set; }

        public static async Task Main(string[] args)
        {
            if (File.Exists(ConfigurePath))
            {
                ApplicationConfig = await LoadConfigAsync().ConfigureAwait(false);
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
            DiscordClient.Log += RequestLogAsync;
            DiscordClient.Ready += () => Task.WhenAny(NotificationManager.InitAsync(), MonitorManager.WorkAsync(), TickerManager.WorkAsync(), Task.Delay(0));

            await ModuleManager.InstallAsync().ConfigureAwait(false);

            await DiscordClient.LoginAsync(TokenType.Bot, ApplicationConfig.DiscordToken).ConfigureAwait(false);
            await DiscordClient.StartAsync().ConfigureAwait(false);

            await Task.Delay(-1).ConfigureAwait(false);
        }

        public static async Task<Config> LoadConfigAsync(string path = ConfigurePath)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                using (var reader = new StreamReader(stream))
                {
                    var result = JsonConvert.DeserializeObject<Config>(await reader.ReadToEndAsync().ConfigureAwait(false));
                    await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "Program", "The config has been saved successfully.")).ConfigureAwait(false);
                    return result;
                }
            }
            catch (Exception ex)
            {
                await RequestLogAsync(new LogMessage(LogSeverity.Error, "Program", ex.Message, ex)).ConfigureAwait(false);
                throw;
            }
        }

        public static async Task SaveConfigAsync(Config config, string path = ConfigurePath)
        {
            try
            {
                using (var stream = File.OpenWrite(path))
                using (var writer = new StreamWriter(stream))
                {
                    await writer.WriteLineAsync(JsonConvert.SerializeObject(config, Formatting.Indented)).ConfigureAwait(false);
                    await RequestLogAsync(new LogMessage(LogSeverity.Verbose, "Program", "The config has been saved successfully.")).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                await RequestLogAsync(new LogMessage(LogSeverity.Error, "Program", ex.Message, ex)).ConfigureAwait(false);
                throw;
            }
        }

        public static async Task LogAsync(LogMessage message)
        {
            while (IsLoggerLocked)
            {
                await Task.Delay(1).ConfigureAwait(false);
            }
            IsLoggerLocked = true;
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
            await Console.Out.WriteLineAsync($"[{message.Source}]{message.Message}").ConfigureAwait(false);
            Console.ResetColor();
            IsLoggerLocked = false;
        }

        public static async Task RequestLogAsync(LogMessage message)
        {
            await Task.WhenAny
            (
                LogAsync(message),
                Task.Delay(0)
            ).ConfigureAwait(false);
        }
    }
}
