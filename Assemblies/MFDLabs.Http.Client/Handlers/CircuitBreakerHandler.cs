using System;
using System.Threading;
using System.Threading.Tasks;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.Client
{
    public class CircuitBreakerHandler : PipelineHandler<IHttpRequest, IHttpResponse>, IDisposable
    {
        public CircuitBreakerHandler(IHttpClientCircuitBreakerProvider httpClientCircuitBreakerProvider) 
            => _HttpClientCircuitBreakerProvider = httpClientCircuitBreakerProvider ?? throw new ArgumentNullException("httpClientCircuitBreakerProvider");
        public CircuitBreakerHandler(ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy)
        {
            if (circuitBreakerPolicy == null) throw new ArgumentNullException(nameof(circuitBreakerPolicy));
            _HttpClientCircuitBreakerProvider = new StaticHttpClientCircuitBreakerProvider(circuitBreakerPolicy);
        }

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            var policy = _HttpClientCircuitBreakerProvider.GetCircuitBreakerPolicy(context.Input);
            policy.ThrowIfTripped(context);
            try
            {
                base.Invoke(context);
                policy.NotifyRequestFinished(context);
            }
            catch (Exception exception)
            {
                policy.NotifyRequestFinished(context, exception);
                throw;
            }
        }
        public override async Task InvokeAsync(IExecutionContext<IHttpRequest, IHttpResponse> context, CancellationToken cancellationToken)
        {
            var policy = _HttpClientCircuitBreakerProvider.GetCircuitBreakerPolicy(context.Input);
            policy.ThrowIfTripped(context);
            try
            {
                await base.InvokeAsync(context, cancellationToken);
                policy.NotifyRequestFinished(context, null);
            }
            catch (Exception exception)
            {
                policy.NotifyRequestFinished(context, exception);
                throw;
            }
        }
        public void Dispose()
        {
            if (_Disposed) return;
            _HttpClientCircuitBreakerProvider.Dispose();
            _Disposed = true;
        }

        private readonly IHttpClientCircuitBreakerProvider _HttpClientCircuitBreakerProvider;
        private bool _Disposed;
    }
}
