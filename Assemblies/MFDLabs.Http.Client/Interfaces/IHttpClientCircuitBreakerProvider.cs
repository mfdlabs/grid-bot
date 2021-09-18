using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;
using System;

namespace MFDLabs.Http.Client
{
    public interface IHttpClientCircuitBreakerProvider : IDisposable
    {
        ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> GetCircuitBreakerPolicy(IHttpRequest httpRequest);
    }
}
