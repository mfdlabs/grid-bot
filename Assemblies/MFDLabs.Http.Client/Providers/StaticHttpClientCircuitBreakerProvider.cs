using System;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.Client
{
    public class StaticHttpClientCircuitBreakerProvider : IHttpClientCircuitBreakerProvider
    {
        public StaticHttpClientCircuitBreakerProvider(ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy) 
            => _circuitBreakerPolicy = circuitBreakerPolicy ?? throw new ArgumentNullException(nameof(circuitBreakerPolicy));

        public ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> GetCircuitBreakerPolicy(IHttpRequest httpRequest)
        {
            if (httpRequest == null) throw new ArgumentNullException(nameof(httpRequest));
            return _circuitBreakerPolicy;
        }
        public void Dispose()
        {
            if (_disposed) return;
            GC.SuppressFinalize(this);
            _circuitBreakerPolicy.Dispose();
            _disposed = true;
        }

        private readonly ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> _circuitBreakerPolicy;
        private bool _disposed;
    }
}
