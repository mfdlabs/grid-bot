using System;

namespace MFDLabs.Sentinels.CircuitBreakerPolicy
{
    public interface ICircuitBreakerPolicy<in TExecutionContext> : IDisposable
    {
        event Action RequestIntendingToOpenCircuitBreaker;

        event Action CircuitBreakerTerminatingRequest;

        void NotifyRequestFinished(TExecutionContext executionContext, Exception exception = null);

        void ThrowIfTripped(TExecutionContext executionContext);
    }
}
