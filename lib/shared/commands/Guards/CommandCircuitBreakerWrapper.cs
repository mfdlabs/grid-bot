using System;
using System.ServiceModel;
using System.Threading.Tasks;

using Discord.WebSocket;

using Polly;
using Polly.CircuitBreaker;

using Grid.Bot.Interfaces;

namespace Grid.Bot.Guards
{
    public class CommandCircuitBreakerWrapper
    {
        private readonly IStateSpecificCommandHandler _command;
        private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

        public TimeSpan RetryInterval { get; set; } = global::Grid.Bot.Properties.Settings.Default.CommandCircuitBreakerWrapperRetryInterval;
        public IStateSpecificCommandHandler Command => _command;
        
        public CommandCircuitBreakerWrapper(IStateSpecificCommandHandler cmd)
        {
            _command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            _circuitBreaker = Policy
                .Handle<Exception>(ex => ex is not (ApplicationException or TimeoutException or EndpointNotFoundException or FaultException))
                .CircuitBreakerAsync(1, RetryInterval);
        }

        public async Task ExecuteAsync(string[] contentArray, SocketMessage message, string ocn)
            => await _circuitBreaker.ExecuteAsync(async () => await _command.Invoke(contentArray, message, ocn));
   
    }

}
