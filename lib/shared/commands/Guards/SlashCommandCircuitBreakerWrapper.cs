#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.Guards;

using System;
using System.ServiceModel;
using System.Threading.Tasks;

using Discord.WebSocket;

using Polly;
using Polly.CircuitBreaker;

using Interfaces;

internal class SlashCommandCircuitBreakerWrapper
{
    private readonly ISlashCommandHandler _command;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public TimeSpan RetryInterval { get; set; } = CommandsSettings.Singleton.CommandCircuitBreakerWrapperRetryInterval;
    public ISlashCommandHandler Command => _command;

    public SlashCommandCircuitBreakerWrapper(ISlashCommandHandler cmd)
    {
        _command = cmd ?? throw new ArgumentNullException(nameof(cmd));
        _circuitBreaker = Policy
            .Handle<Exception>(ex => ex is not (ApplicationException or TimeoutException or EndpointNotFoundException or FaultException))
            .CircuitBreakerAsync(1, RetryInterval);
    }

    public async Task ExecuteAsync(SocketSlashCommand command)
        => await _circuitBreaker.ExecuteAsync(async () => await _command.ExecuteAsync(command));
}


#endif
