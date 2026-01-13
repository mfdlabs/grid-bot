using Grpc.Core;

namespace Grid.Bot.Grpc;

using System;
using System.Linq;
using System.Threading.Tasks;

using Prometheus;

using Discord;
using Discord.WebSocket;

using V1;

/// <summary>
/// The Grid Bot gRPC server implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GridBotGrpcServer"/> class.
/// </remarks>
/// <param name="client">The <see cref="DiscordShardedClient"/> instance.</param>
/// <exception cref="ArgumentNullException">Thrown when the <paramref name="client"/> is null.</exception>
public class GridBotGrpcServer(DiscordShardedClient client) : GridBotAPI.GridBotAPIBase
{
    private readonly DiscordShardedClient _client = client ?? throw new ArgumentNullException(nameof(client));

    private static readonly Counter GrpcServerRequestCounter = Metrics.CreateCounter(
        "grpc_health_check_requests_total",
        "Total number of gRPC health check requests"
    );

    /// <inheritdoc cref="GridBotAPI.GridBotAPIBase.CheckHealth(CheckHealthRequest, ServerCallContext)"/>
    public override Task<CheckHealthResponse> CheckHealth(CheckHealthRequest request, ServerCallContext context)
    {
        GrpcServerRequestCounter.Inc();

        var response = new CheckHealthResponse();

        if (_client.LoginState is LoginState.LoggedOut or LoginState.LoggingOut or LoginState.LoggingIn)
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
}
