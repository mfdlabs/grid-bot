using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public abstract class CircuitBreakerPolicyBase<TExecutionContext> : ICircuitBreakerPolicy<TExecutionContext>
    {
        protected CircuitBreakerPolicyBase(ITripReasonAuthority<TExecutionContext> tripReasonAuthority) 
            => _tripReasonAuthority = tripReasonAuthority ?? throw new ArgumentNullException(nameof(tripReasonAuthority));

        public event Action RequestIntendingToOpenCircuitBreaker;
        public event Action CircuitBreakerTerminatingRequest;

        public void NotifyRequestFinished(TExecutionContext executionContext, Exception exception)
        {
            try
            {
                if (_tripReasonAuthority.IsReasonForTrip(executionContext, exception))
                {
                    RequestIntendingToOpenCircuitBreaker?.Invoke();
                    TryToTripCircuitBreaker(executionContext);
                }
                else
                    OnSuccessfulRequest(executionContext);
            }
            finally { OnNotified(executionContext); }
        }
        public void ThrowIfTripped(TExecutionContext executionContext)
        {
            if (!IsCircuitBreakerOpened(executionContext, out var ex)) return;
            CircuitBreakerTerminatingRequest?.Invoke();
            throw ex;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected abstract bool IsCircuitBreakerOpened(TExecutionContext executionContext, out CircuitBreakerException exception);
        protected abstract bool TryToTripCircuitBreaker(TExecutionContext executionContext);
        protected abstract void OnSuccessfulRequest(TExecutionContext executionContext);
        protected abstract void OnNotified(TExecutionContext executionContext);
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing && _tripReasonAuthority is IDisposable authority) authority.Dispose();
            _disposed = true;
        }

        private readonly ITripReasonAuthority<TExecutionContext> _tripReasonAuthority;
        private bool _disposed;
    }
}
