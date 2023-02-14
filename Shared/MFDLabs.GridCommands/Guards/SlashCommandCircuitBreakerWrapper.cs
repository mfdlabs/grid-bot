#if WE_LOVE_EM_SLASH_COMMANDS

using System;
using System.ServiceModel;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Sentinels;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Guards
{
    public class SlashCommandCircuitBreakerWrapper
    {
        public TimeSpan RetryInterval { get; set; } = global::MFDLabs.Grid.Bot.Properties.Settings.Default.CommandCircuitBreakerWrapperRetryInterval;
        public IStateSpecificSlashCommandHandler Command => _command;

        public SlashCommandCircuitBreakerWrapper(IStateSpecificSlashCommandHandler cmd)
        {
            _command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            _circuitBreaker = new($"Slash Command '{cmd.CommandAlias}' Circuit Breaker", ex => ex is not (ApplicationException or TimeoutException or EndpointNotFoundException or FaultException), () => RetryInterval);
        }
        public async Task ExecuteAsync(SocketSlashCommand command)
            => await _circuitBreaker.ExecuteAsync(async _ => await _command.Invoke(command));

        private readonly IStateSpecificSlashCommandHandler _command;
        private readonly ExecutionCircuitBreaker _circuitBreaker;
    }

}

#endif
