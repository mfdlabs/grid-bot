using MFDLabs.Text.Extensions;
using System;
using System.Collections.Concurrent;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Http.Client
{
    public class DynamicHttpClientCircuitBreakerProvider : IHttpClientCircuitBreakerProvider, IDisposable
    {
        public event Action<ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>> CircuitBreakerPolicyCreated;

        public DynamicHttpClientCircuitBreakerProvider(IDefaultCircuitBreakerPolicyConfig circuitBreakerPolicyConfig, Func<ClientCircuitBreakerType> circuitBreakerTypeGetter, string circuitBreakerIdentifier)
        {
            if (circuitBreakerIdentifier.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Value cannot be null or whitespace.", "circuitBreakerIdentifier");
            }

            _CircuitBreakerPolicyConfig = circuitBreakerPolicyConfig ?? throw new ArgumentNullException("circuitBreakerPolicyConfig");
            _CircuitBreakerTypeGetter = circuitBreakerTypeGetter ?? throw new ArgumentNullException("circuitBreakerTypeGetter");
            _CircuitBreakerIdentifier = circuitBreakerIdentifier;
            _TripReasonAuthority = new DefaultTripReasonAuthority();
            _CircuitBreakerPolicies = new ConcurrentDictionary<string, ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>>();
        }

        public ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> GetCircuitBreakerPolicy(IHttpRequest httpRequest)
        {
            if (httpRequest == null)
            {
                throw new ArgumentNullException("httpRequest");
            }
            return _CircuitBreakerPolicies.GetOrAdd(GetCircuitBreakerPolicyKey(httpRequest), CreateCircuitBreakerPolicy);
        }

        public void Dispose()
        {
            if (_Disposed)
            {
                return;
            }
            foreach (var policy in _CircuitBreakerPolicies.Values)
            {
                policy.Dispose();
            }
            _Disposed = true;
        }

        private ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> CreateCircuitBreakerPolicy(string circuitBreakerPolicyKey)
        {
            var policy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(_CircuitBreakerIdentifier, _CircuitBreakerPolicyConfig, _TripReasonAuthority);
            CircuitBreakerPolicyCreated?.Invoke(policy);
            return policy;
        }

        private string GetCircuitBreakerPolicyKey(IHttpRequest httpRequest)
        {
            var type = _CircuitBreakerTypeGetter();
            if (type == ClientCircuitBreakerType.WholeClient)
            {
                return _WholeClientCircuitBreakerKey;
            }
            if (type != ClientCircuitBreakerType.PerEndpoint)
            {
                throw new NotSupportedException($"The ClientCircuitBreakerType ({type}) has not yet been implemented.");
            }
            return httpRequest.Url.AbsolutePath;
        }

        private const string _WholeClientCircuitBreakerKey = "__WholeClient";

        private readonly string _CircuitBreakerIdentifier;

        private readonly Func<ClientCircuitBreakerType> _CircuitBreakerTypeGetter;

        private readonly IDefaultCircuitBreakerPolicyConfig _CircuitBreakerPolicyConfig;

        private readonly ITripReasonAuthority<IExecutionContext<IHttpRequest, IHttpResponse>> _TripReasonAuthority;

        private readonly ConcurrentDictionary<string, ICircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>> _CircuitBreakerPolicies;

        private bool _Disposed;
    }
}
