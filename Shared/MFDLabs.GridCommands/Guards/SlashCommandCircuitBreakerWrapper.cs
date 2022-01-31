#if WE_LOVE_EM_SLASH_COMMANDS
using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Sentinels;

namespace MFDLabs.Grid.Bot.Guards
{


    public class SlashCommandCircuitBreakerWrapper
    {
        public TimeSpan RetryInterval { get; set; }
        public TimeSpan TimeoutIntervalForTripping { get; set; }
        public IStateSpecificSlashCommandHandler Command => _Command;

        public SlashCommandCircuitBreakerWrapper(IStateSpecificSlashCommandHandler cmd)
        {
            _Command = cmd ?? throw new ArgumentNullException(nameof(cmd));
            _CircuitBreaker = new CircuitBreaker(GetType().Name);
        }
        private void ResetCircuitBreaker()
        {
            _CircuitBreaker.Reset();
            _NextRetry = DateTime.MinValue;
        }
        private void ResetSignal() => Interlocked.Exchange(ref _ShouldRetrySignal, 0);
        private void TripAndRethrow(Exception ex)
        {
            var now = DateTime.UtcNow;
            _NextRetry = now.Add(RetryInterval);
            _CircuitBreaker.Trip();
            throw ex;
        }
        private void Test()
        {
            try { _CircuitBreaker.Test(); }
            catch (CircuitBreakerException)
            {
                if (!(DateTime.UtcNow >= _NextRetry)) throw;
                if (Interlocked.CompareExchange(ref _ShouldRetrySignal, 1, 0) != 0) throw;
            }
        }
        public void Execute(SocketSlashCommand cmd)
        {
            Test();
            try { _Command.Invoke(cmd).Wait(); }
            catch (Exception ex) { TripAndRethrow(ex); }
            finally { ResetSignal(); }
            ResetCircuitBreaker();
        }
        public async Task ExecuteAsync(SocketSlashCommand cmd)
        {
            Test();
            try { await _Command.Invoke(cmd).ConfigureAwait(false); }
            catch (Exception ex) { TripAndRethrow(ex); }
            finally { ResetSignal(); }
            ResetCircuitBreaker();
        }



        private readonly IStateSpecificSlashCommandHandler _Command;
        private readonly CircuitBreaker _CircuitBreaker;
        private DateTime _NextRetry = DateTime.MinValue;
        private int _ShouldRetrySignal;
    }
}

#endif
