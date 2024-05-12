namespace Grid.Bot;

using System;
using System.Threading.Tasks;

using Grpc.Core;

using Logging;

using V1;

/// <summary>
/// Worker for the bot check.
/// </summary>
/// <remarks>
/// Construct a new instance of <see cref="BotCheckWorker"/>.
/// </remarks>
/// <param name="client">The <see cref="GridBotAPI.GridBotAPIClient"/>.</param>
/// <param name="botManager">The <see cref="IBotManager"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <param name="settings">The <see cref="ISettings"/>.</param>
/// <param name="discordWebhookAlertManager">The <see cref="IDiscordWebhookAlertManager"/>.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="botManager"/> cannot be null.
/// - <paramref name="logger"/> cannot be null.
/// - <paramref name="settings"/> cannot be null.
/// - <paramref name="discordWebhookAlertManager"/> cannot be null.
/// </exception>
public class BotCheckWorker(
    GridBotAPI.GridBotAPIClient client,
    IBotManager botManager,
    ILogger logger,
    ISettings settings,
    IDiscordWebhookAlertManager discordWebhookAlertManager
)
{
    private readonly GridBotAPI.GridBotAPIClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly IBotManager _botManager = botManager ?? throw new ArgumentNullException(nameof(botManager));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ISettings _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    private readonly IDiscordWebhookAlertManager _discordWebhookAlertManager = discordWebhookAlertManager ?? throw new ArgumentNullException(nameof(discordWebhookAlertManager));

    private int _continousFailures = 0;
    private CheckHealthResponse _lastHealthCheckResponse = null;
    private bool _botRunning = false;

    /// <summary>
    /// Start the worker.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task StartAsync()
    {
        return Task.Factory.StartNew(async () =>
        {
            while (true)
            {
                if (await CallToHealthCheck())
                    await Reset();
                else
                {
                    _continousFailures++;

                    _logger.Warning(
                        "Bot check failed {0} times.",
                        _continousFailures
                    );

                    _discordWebhookAlertManager.SendAlertAsync(
                        "Bot Check Failure",
                        $"Bot check failed {_continousFailures} times. Last health check response: Latency = {_lastHealthCheckResponse?.Latency}, Status = {_lastHealthCheckResponse?.Status}, Shards = {string.Join(", ", _lastHealthCheckResponse?.Shards)}"
                    );

                    if (_continousFailures >= _settings.MaxContinuousFailures && !_botRunning)
                    {
                        _logger.Warning(
                            "The continuous failures have reached the limit of {0}, starting bot.",
                            _settings.MaxContinuousFailures
                        );

                        _discordWebhookAlertManager.SendAlertAsync(
                            "Bot Check Failure",
                            $"The continuous failures have reached the limit of {_settings.MaxContinuousFailures}, starting bot."
                        );

                        await _botManager.StartAsync();

                        _botRunning = true;
                    }
                }

                await Task.Delay(_settings.BotCheckWorkerDelay);
            }
        }, TaskCreationOptions.LongRunning);
    }

    private async Task Reset()
    {
        _continousFailures = 0;

        if (_botRunning)
        {
            _logger.Warning("Bot check succeeded, stopping bot.");

            _discordWebhookAlertManager.SendAlertAsync(
                "Bot Check Success",
                "Bot check succeeded, stopping bot."
            );

            await _botManager.StopAsync();

            _botRunning = false;
        }
    }

    private async Task<bool> CallToHealthCheck()
    {
        var request = new CheckHealthRequest();
        var callOptions = new CallOptions(deadline: DateTime.UtcNow.AddSeconds(3));

        try
        {
            // Okay means it doesn't throw an exception.
            _lastHealthCheckResponse = await _client.CheckHealthAsync(request, callOptions);

            _logger.Debug(
                "Bot check health, Latency = {0}, Status = {1}, Shards = {2}",
                _lastHealthCheckResponse.Latency,
                _lastHealthCheckResponse.Status,
                string.Join(", ", _lastHealthCheckResponse.Shards)
            );

            return true;
        }
        catch (RpcException ex)
        {
            _logger.Debug(
                "Failed to check health: {0}",
                ex.Message
            );

            return false;
        }
    }
}
