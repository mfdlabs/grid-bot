using System;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.Client
{
    public class StaticHttpClientCircuitBreakerProvider : IHttpClientCircuitBreakerProvider, IDisposable
    {
        public StaticHttpClientCircuitBreakerProvider(ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy) 
            => _CircuitBreakerPolicy = circuitBreakerPolicy ?? throw new ArgumentNullException("circuitBreakerPolicy");

        public ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> GetCircuitBreakerPolicy(IHttpRequest httpRequest)
        {
            if (httpRequest == null) throw new ArgumentNullException(nameof(httpRequest));
            return _CircuitBreakerPolicy;
        }
        public void Dispose()
        {
            if (_Disposed) return;
            _CircuitBreakerPolicy.Dispose();
            _Disposed = true;
        }

        private readonly ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> _CircuitBreakerPolicy;
        private bool _Disposed;
    }
}
