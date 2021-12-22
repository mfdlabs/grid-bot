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
            => _httpClientCircuitBreakerProvider = httpClientCircuitBreakerProvider ?? throw new ArgumentNullException(nameof(httpClientCircuitBreakerProvider));
        public CircuitBreakerHandler(ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy)
        {
            if (circuitBreakerPolicy == null) throw new ArgumentNullException(nameof(circuitBreakerPolicy));
            _httpClientCircuitBreakerProvider = new StaticHttpClientCircuitBreakerProvider(circuitBreakerPolicy);
        }

        public override void Invoke(IExecutionContext<IHttpRequest, IHttpResponse> context)
        {
            var policy = _httpClientCircuitBreakerProvider.GetCircuitBreakerPolicy(context.Input);
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
            var policy = _httpClientCircuitBreakerProvider.GetCircuitBreakerPolicy(context.Input);
            policy.ThrowIfTripped(context);
            try
            {
                await base.InvokeAsync(context, cancellationToken);
                policy.NotifyRequestFinished(context);
            }
            catch (Exception exception)
            {
                policy.NotifyRequestFinished(context, exception);
                throw;
            }
        }
        public void Dispose()
        {
            if (_disposed) return;
            GC.SuppressFinalize(this);
            _httpClientCircuitBreakerProvider.Dispose();
            _disposed = true;
        }

        private readonly IHttpClientCircuitBreakerProvider _httpClientCircuitBreakerProvider;
        private bool _disposed;
    }
}
