using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public abstract class CircuitBreakerPolicyBase<TExecutionContext> : ICircuitBreakerPolicy<TExecutionContext>, IDisposable
    {
        protected CircuitBreakerPolicyBase(ITripReasonAuthority<TExecutionContext> tripReasonAuthority) 
            => _TripReasonAuthority = tripReasonAuthority ?? throw new ArgumentNullException("tripReasonAuthority");

        public event Action RequestIntendingToOpenCircuitBreaker;
        public event Action CircuitBreakerTerminatingRequest;

        public void NotifyRequestFinished(TExecutionContext executionContext, Exception exception)
        {
            try
            {
                if (_TripReasonAuthority.IsReasonForTrip(executionContext, exception))
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
            if (!IsCircuitBreakerOpened(executionContext, out CircuitBreakerException ex)) return;
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
            if (_Disposed) return;
            if (disposing && _TripReasonAuthority is IDisposable authority) authority.Dispose();
            _Disposed = true;
        }

        private readonly ITripReasonAuthority<TExecutionContext> _TripReasonAuthority;
        private bool _Disposed;
    }
}
