#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.ServiceModel;
using System.Threading.Tasks;

using Discord.WebSocket;

using Polly;
using Polly.CircuitBreaker;

using Grid.Bot.Interfaces;

namespace Grid.Bot.Guards
{
    public class SlashCommandCircuitBreakerWrapper
    {
        private readonly IStateSpecificSlashCommandHandler _command;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

        public TimeSpan RetryInterval { get; set; } = global::Grid.Bot.Properties.Settings.Default.CommandCircuitBreakerWrapperRetryInterval;
        public IStateSpecificSlashCommandHandler Command => _command;

        public SlashCommandCircuitBreakerWrapper(IStateSpecificSlashCommandHandler cmd)
        {
            _command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            _circuitBreaker = Policy
                .Handle<Exception>(ex => ex is not (ApplicationException or TimeoutException or EndpointNotFoundException or FaultException))
                .CircuitBreakerAsync(1, RetryInterval);
        }

        public async Task ExecuteAsync(SocketSlashCommand command)
            => await _circuitBreaker.ExecuteAsync(async () => await _command.Invoke(command));
    }

}

#endif
