namespace Grid.Bot.Extensions;

using System;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Interactions;

using Microsoft.Extensions.DependencyInjection;

using Random;
using Logging;

using Events;
using Utility;
using Prometheus;

/// <summary>
/// Extension methods for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <param name="services">The <see cref="IServiceProvider"/>.</param>
    extension(IServiceProvider services)
    {
        /// <summary>
        /// Collects all log files and uploads them to Backtrace.
        /// </summary>
        public void UploadAllLogFilesToBacktrace()
        {
            var logger = services.GetRequiredService<ILogger>();
            var backtraceSettings = services.GetRequiredService<BacktraceSettings>();
            var percentageInvoker = services.GetRequiredService<IPercentageInvoker>();
            var backtraceUtility = services.GetService<IBacktraceUtility>();

            if (backtraceUtility == null) return;

            try
            {
                percentageInvoker.InvokeAction(
                    () => backtraceUtility.UploadAllLogFiles(),
                    backtraceSettings.UploadLogFilesToBacktraceEnabledPercent
                );
            }
            catch (Exception ex)
            {
                logger.Warning($"Log file upload to Backtrace failed: {ex}");
            }
        }

        /// <summary>
        /// Adds all needed event handlers for Discord and attempts to start the client.
        /// </summary>
        public async Task UseDiscordAsync()
        {
            var client = services.GetRequiredService<DiscordShardedClient>();
            var interactions = services.GetRequiredService<InteractionService>();
            var commands = services.GetRequiredService<CommandService>();

            var onLogMessage = services.GetRequiredService<OnLogMessage>();
            var onShardReady = services.GetRequiredService<OnShardReady>();

            client.Log += onLogMessage.Invoke;
            interactions.Log += onLogMessage.Invoke;
            commands.Log += onLogMessage.Invoke;

            client.ShardReady += onShardReady.Invoke;

            var discordSettings = services.GetRequiredService<DiscordSettings>();

            if (string.IsNullOrEmpty(discordSettings.BotToken))
            {
                var logger = services.GetRequiredService<ILogger>();
                logger.Warning("Discord bot token is not set! Cannot start Discord client.");

                return;
            }

            await client.LoginAsync(TokenType.Bot, discordSettings.BotToken).ConfigureAwait(false);
            await client.StartAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the metrics server based on global settings.
        /// </summary>
        public void UseMetricsServer()
        {
            var globalSettings = services.GetRequiredService<GlobalSettings>();

            // Extract host and port from bind address
            var bindAddress = globalSettings.MetricsBindAddress;
            var host = bindAddress.Split(':')[1].TrimStart('/');
            var port = int.Parse(bindAddress.Split(':')[2].TrimEnd('/'));

            new KestrelMetricServer(
                hostname: host,
                port: port
            ).Start();
        }
    }
}
