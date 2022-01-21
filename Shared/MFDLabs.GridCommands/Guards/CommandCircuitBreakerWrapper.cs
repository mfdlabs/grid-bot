using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Guards;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Sentinels;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Grid.Bot.Guards
{
    public class CommandCircuitBreakerWrapper
    {
        public TimeSpan RetryInterval { get; set; }
        public TimeSpan TimeoutIntervalForTripping { get; set; }
        public IStateSpecificCommandHandler Command => _Command;
        
        public CommandCircuitBreakerWrapper(IStateSpecificCommandHandler cmd)
        {
            _Command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            _CircuitBreaker = new CircuitBreaker(GetType().Name);
        }
        private void ResetCircuitBreaker()
        {
            _CircuitBreaker.Reset();
            _NextRetry = DateTime.MinValue;
        }
        private void ResetSignal()
        {
            Interlocked.Exchange(ref _ShouldRetrySignal, 0);
        }
        private void ResetTimeoutCount()
        {
            Interlocked.Exchange(ref _TimeoutCount, 0);
            DateTime.UtcNow.Add(TimeoutIntervalForTripping);
        }
        private void TripAndRethrow(Exception ex)
        {
            var now = DateTime.UtcNow;
            _NextRetry = now.Add(RetryInterval);
            _CircuitBreaker.Trip();
            throw ex;
        }
        private void Test()
        {
            try
            {
                _CircuitBreaker.Test();
            }
            catch (CircuitBreakerException)
            {
                if (!(DateTime.UtcNow >= _NextRetry))
                {
                    throw;
                }
                if (Interlocked.CompareExchange(ref _ShouldRetrySignal, 1, 0) != 0)
                {
                    throw;
                }
            }
        }
        public void Execute(string[] contentArray, SocketMessage message, string ocn)
        {
            Test();
            try
            {
                _Command.Invoke(contentArray, message, ocn).Wait();
            }
            catch (Exception ex)
            {
                TripAndRethrow(ex);
            }
            finally
            {
                ResetSignal();
            }
            ResetCircuitBreaker();
        }
        public async Task ExecuteAsync(string[] contentArray, SocketMessage message, string ocn)
        {
            Test();
            try
            {
                await _Command.Invoke(contentArray, message, ocn).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                TripAndRethrow(ex);
            }
            finally
            {
                ResetSignal();
            }
            ResetCircuitBreaker();
        }
        
        

        private readonly IStateSpecificCommandHandler _Command;
        private readonly CircuitBreaker _CircuitBreaker;
        private DateTime _NextRetry = DateTime.MinValue;
        private int _ShouldRetrySignal;
        private int _TimeoutCount;
    }
}