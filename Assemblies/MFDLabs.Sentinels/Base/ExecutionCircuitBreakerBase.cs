using System;
using System.Threading;
using System.Threading.Tasks;

namespace MFDLabs.Sentinels
{
    public abstract class ExecutionCircuitBreakerBase : CircuitBreakerBase
    {
        private bool IsTimeForRetry => Now >= _nextRetry;
        protected abstract TimeSpan RetryInterval { get; }

        private bool ShouldRetry() => Interlocked.CompareExchange(ref _shouldRetrySignal, 1, 0) == 0;
        private void AttemptToProceed()
        {
            try { Test(); }
            catch (CircuitBreakerException) { if (!IsTimeForRetry || !ShouldRetry()) throw; }
        }
        protected abstract bool ShouldTrip(Exception ex);
        public void Execute(Action action)
        {
            AttemptToProceed();
            try { action(); }
            catch (Exception ex)
            {
                if (!ShouldTrip(ex)) throw;
                _nextRetry = Now.Add(RetryInterval);
                Trip();
                throw;
            }
            finally { Interlocked.Exchange(ref _shouldRetrySignal, 0); }
            Reset();
        }
        public async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
        {
            AttemptToProceed();
            try { await action(cancellationToken).ConfigureAwait(false); }
            catch (Exception ex)
            {
                if (!ShouldTrip(ex)) throw;
                _nextRetry = Now.Add(RetryInterval);
                Trip();
                throw;
            }
            finally { Interlocked.Exchange(ref _shouldRetrySignal, 0); }
            Reset();
        }
        public override bool Reset()
        {
            var result = base.Reset();
            _nextRetry = DateTime.MinValue;
            return result;
        }

        private DateTime _nextRetry = DateTime.MinValue;
        private int _shouldRetrySignal;
    }
}
