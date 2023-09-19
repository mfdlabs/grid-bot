namespace Grid.Bot.Guards;

using System;
using System.ServiceModel;
using System.Threading.Tasks;

using Discord.WebSocket;

using Polly;
using Polly.CircuitBreaker;

using Interfaces;

internal class CommandCircuitBreakerWrapper
{
    private readonly ICommandHandler _command;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public TimeSpan RetryInterval { get; set; } = CommandsSettings.Singleton.CommandCircuitBreakerWrapperRetryInterval;
    public ICommandHandler Command => _command;

    public CommandCircuitBreakerWrapper(ICommandHandler cmd)
    {
        _command = cmd ?? throw new ArgumentNullException(nameof(cmd));
        _circuitBreaker = Policy
            .Handle<Exception>(ex => ex is not (ApplicationException or TimeoutException or EndpointNotFoundException or FaultException))
            .CircuitBreakerAsync(1, RetryInterval);
    }

    public async Task ExecuteAsync(string[] contentArray, SocketMessage message, string ocn)
        => await _circuitBreaker.ExecuteAsync(async () => await _command.ExecuteAsync(contentArray, message, ocn));

}

