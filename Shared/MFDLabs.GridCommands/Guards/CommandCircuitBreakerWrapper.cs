using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Sentinels;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Guards
{
    public class CommandCircuitBreakerWrapper
    {
        public TimeSpan RetryInterval { get; set; } = global::MFDLabs.Grid.Bot.Properties.Settings.Default.CommandCircuitBreakerWrapperRetryInterval;
        public IStateSpecificCommandHandler Command => _command;
        
        public CommandCircuitBreakerWrapper(IStateSpecificCommandHandler cmd)
        {
            _command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            _circuitBreaker = new($"Command '{cmd.CommandName}' Circuit Breaker", ex => ex is not ApplicationException, () => RetryInterval);
        }
        public async Task ExecuteAsync(string[] contentArray, SocketMessage message, string ocn)
            => await _circuitBreaker.ExecuteAsync(async _ => await _command.Invoke(contentArray, message, ocn));
   
        private readonly IStateSpecificCommandHandler _command;
        private readonly ExecutionCircuitBreaker _circuitBreaker;
    }

}