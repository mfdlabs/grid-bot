using System;
using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Http.Client.Monitoring;
using MFDLabs.Http.ServiceClient;
using MFDLabs.Instrumentation;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;
using MFDLabs.Text.Extensions;

using HttpClientBuilder = MFDLabs.Http.Client.HttpClientBuilder;

namespace MFDLabs.Analytics.Google.Client
{
    internal sealed class GaHttpClientBuilder : HttpClientBuilder
    {
        public GaHttpClientBuilder(ICounterRegistry counterRegistry,
            GaClientSettings httpClientSettings,
            GaClientConfig config)
            : base(null, httpClientSettings)
        {
            if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
            if (httpClientSettings == null) throw new ArgumentNullException(nameof(httpClientSettings));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (httpClientSettings.ClientName.IsNullOrWhiteSpace()) 
                throw new ArgumentException("httpClientSettings.ClientName value has to be a non-empty string.", nameof(httpClientSettings));
            
            AppendHandler(new OperationErrorHandler());
            AddHandlerBefore<SendHttpRequestHandler>(new HttpRequestMetricsHandler(counterRegistry, CategoryName, httpClientSettings.ClientName));

            var circuitBreakerPolicy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(
                ClientCircuitBreakerPart + httpClientSettings.ClientName,
                new DefaultCircuitBreakerPolicyConfig
                {
                    FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip,
                    RetryInterval = config.CircuitBreakerRetryInterval
                },
                new DefaultTripReasonAuthority()
            );

            new CircuitBreakerPolicyMetricsEventHandler(counterRegistry).RegisterEvents(circuitBreakerPolicy, CategoryName, httpClientSettings.ClientName);
            AddHandlerAfter<RequestFailureThrowsHandler>(new CircuitBreakerHandler(circuitBreakerPolicy));
        }

        private const string ClientCircuitBreakerPart = "MFDLabs.Analytics.Google.Client.";
        private const string CategoryName = "MFDLabs.Analytics.Google.Client";
    }
}
