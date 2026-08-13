using Grpc.Core;

namespace Grid.Bot.Grpc;

using System;
using System.Linq;
using System.Threading.Tasks;

using Prometheus;

using Discord;
using Discord.WebSocket;

using Google.Protobuf.WellKnownTypes;

using V1;

/// <summary>
/// The Grid Bot gRPC server implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GridBotGrpcServer"/> class.
/// </remarks>
/// <param name="client">The <see cref="DiscordShardedClient"/> instance.</param>
/// <param name="maintenanceSettings">The <see cref="MaintenanceSettings"/> instance.</param>
/// <param name="discordSettings">The <see cref="DiscordSettings"/> instance.</param>
/// <exception cref="ArgumentNullException">
/// - <paramref name="client"/> cannot be null.
/// - <paramref name="maintenanceSettings"/> cannot be null.
/// - <paramref name="discordSettings"/> cannot be null.
/// </exception>
public class GridBotGrpcServer(
    DiscordShardedClient client, 
    MaintenanceSettings maintenanceSettings,
    DiscordSettings discordSettings
) : GridBotAPI.GridBotAPIBase
{
    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly MaintenanceSettings _maintenanceSettings = maintenanceSettings ?? throw new ArgumentNullException(nameof(maintenanceSettings));
    private readonly DiscordSettings _discordSettings = discordSettings ?? throw new ArgumentNullException(nameof(discordSettings));

    private static readonly Counter _grpcServerRequestCounter = Metrics.CreateCounter(
        "grpc_health_check_requests_total",
        "Total number of gRPC health check requests"
    );

    private static readonly Counter _grpcServerSetStatusRequestCounter = Metrics.CreateCounter(
        "grpc_set_status_requests_total",
        "Total number of gRPC set status requests"
    );


    private static string GetStatusText(string updateText)
        => string.IsNullOrEmpty(updateText) ? "Maintenance is enabled" : $"Maintenance is enabled: {updateText}";

    /// <inheritdoc cref="GridBotAPI.GridBotAPIBase.CheckHealth(Empty, ServerCallContext)"/>
    public override Task<CheckHealthResponse> CheckHealth(Empty request, ServerCallContext context)
    {
        _grpcServerRequestCounter.Inc();

        var response = new CheckHealthResponse();

        if (_client.LoginState == LoginState.LoggedOut || _client.LoginState == LoginState.LoggingOut || _client.LoginState == LoginState.LoggingIn)
            return Task.FromResult(response);

        try
        {
            response.Status = _client.Status.ToString();
            response.Latency = _client.Latency;
            response.Shards.AddRange(_client.Shards.Select(x => x.ShardId.ToString()).ToList());
        }
        catch (Exception)
        {
            response.Status = "error";
            response.Latency = 0;
        }

        return Task.FromResult(response);
    }

    /// <inheritdoc cref="GridBotAPI.GridBotAPIBase.SetStatus(SetStatusRequest, ServerCallContext)"/>
    public override Task<Empty> SetStatus(SetStatusRequest request, ServerCallContext context)
    {
        _grpcServerSetStatusRequestCounter.Inc();

        using (_maintenanceSettings.BeginTransaction())
        {
            _maintenanceSettings.MaintenanceEnabled = request.MaintenanceEnabled;

            if (!string.IsNullOrEmpty(request.MaintenanceMessage) && !_maintenanceSettings.MaintenanceStatus.Equals(request.MaintenanceMessage, StringComparison.InvariantCulture))
                _maintenanceSettings.MaintenanceStatus = request.MaintenanceMessage;
        }

        if (_maintenanceSettings.MaintenanceEnabled)
        {
            _client.SetStatusAsync(UserStatus.DoNotDisturb);
            _client.SetGameAsync(GetStatusText(_maintenanceSettings.MaintenanceStatus));
        }
        else
        {
            _client.SetStatusAsync(_discordSettings.BotStatus);
            if (!string.IsNullOrEmpty(_discordSettings.BotStatusMessage))
                _client.SetGameAsync(_discordSettings.BotStatusMessage);
        }

        return Task.FromResult(new Empty());
    }
}