using MFDLabs.Text.Extensions;
using System;
using System.Collections.Concurrent;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.Client
{
    public class DynamicHttpClientCircuitBreakerProvider : IHttpClientCircuitBreakerProvider
    {
        public event Action<ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>> CircuitBreakerPolicyCreated;

        public DynamicHttpClientCircuitBreakerProvider(IDefaultCircuitBreakerPolicyConfig circuitBreakerPolicyConfig,
            Func<ClientCircuitBreakerType> circuitBreakerTypeGetter,
            string circuitBreakerIdentifier)
        {
            if (circuitBreakerIdentifier.IsNullOrWhiteSpace()) throw new ArgumentException("Value cannot be null or whitespace.", nameof(circuitBreakerIdentifier));

            _circuitBreakerPolicyConfig = circuitBreakerPolicyConfig ?? throw new ArgumentNullException(nameof(circuitBreakerPolicyConfig));
            _circuitBreakerTypeGetter = circuitBreakerTypeGetter ?? throw new ArgumentNullException(nameof(circuitBreakerTypeGetter));
            _circuitBreakerIdentifier = circuitBreakerIdentifier;
            _tripReasonAuthority = new DefaultTripReasonAuthority();
            _circuitBreakerPolicies = new ConcurrentDictionary<string, ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>>();
        }

        public ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> GetCircuitBreakerPolicy(IHttpRequest httpRequest)
        {
            if (httpRequest == null) throw new ArgumentNullException(nameof(httpRequest));
            return _circuitBreakerPolicies.GetOrAdd(GetCircuitBreakerPolicyKey(httpRequest), CreateCircuitBreakerPolicy);
        }
        public void Dispose()
        {
            if (_disposed) return;
            foreach (var policy in _circuitBreakerPolicies.Values) policy.Dispose();
            _disposed = true;
        }
        private ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> CreateCircuitBreakerPolicy(string circuitBreakerPolicyKey)
        {
            var policy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(_circuitBreakerIdentifier, _circuitBreakerPolicyConfig, _tripReasonAuthority);
            CircuitBreakerPolicyCreated?.Invoke(policy);
            return policy;
        }
        private string GetCircuitBreakerPolicyKey(IHttpRequest httpRequest)
        {
            var type = _circuitBreakerTypeGetter();
            if (type == ClientCircuitBreakerType.WholeClient) return WholeClientCircuitBreakerKey;
            if (type != ClientCircuitBreakerType.PerEndpoint) throw new NotSupportedException($"The ClientCircuitBreakerType ({type}) has not yet been implemented.");
            return httpRequest.Url.AbsolutePath;
        }

        private const string WholeClientCircuitBreakerKey = "__WholeClient";
        private readonly string _circuitBreakerIdentifier;
        private readonly Func<ClientCircuitBreakerType> _circuitBreakerTypeGetter;
        private readonly IDefaultCircuitBreakerPolicyConfig _circuitBreakerPolicyConfig;
        private readonly ITripReasonAuthority<IExecutionContext<IHttpRequest, IHttpResponse>> _tripReasonAuthority;
        private readonly ConcurrentDictionary<string, ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>> _circuitBreakerPolicies;
        private bool _disposed;
    }
}
