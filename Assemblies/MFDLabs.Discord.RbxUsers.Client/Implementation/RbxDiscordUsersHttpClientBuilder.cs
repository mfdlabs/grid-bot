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

namespace MFDLabs.Discord.RbxUsers.Client
{
    internal sealed class RbxDiscordUsersHttpClientBuilder : HttpClientBuilder
    {
        public RbxDiscordUsersHttpClientBuilder(ICounterRegistry counterRegistry, RbxDiscordUsersClientSettings httpClientSettings, RbxDiscordUsersClientConfig config) : base(null, httpClientSettings, null)
        {
            if (counterRegistry == null)
            {
                throw new ArgumentNullException("counterRegistry");
            }
            if (httpClientSettings == null)
            {
                throw new ArgumentNullException("httpClientSettings");
            }
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (httpClientSettings.ClientName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("httpClientSettings.ClientName value has to be a non-empty string.", "httpClientSettings");
            }
            AppendHandler(new OperationErrorHandler());

            HttpRequestMetricsHandler metricsHandler = new HttpRequestMetricsHandler(counterRegistry, _CategoryName, httpClientSettings.ClientName);

            AddHandlerBefore<SendHttpRequestHandler>(metricsHandler);
            string circuitBreakerIdentifier = _ClientCircuitBreakerPart + httpClientSettings.ClientName;
            var circuitBreakerPolicyConfig = new DefaultCircuitBreakerPolicyConfig
            {
                FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip,
                RetryInterval = config.CircuitBreakerRetryInterval
            };
            var circuitBreakerPolicy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(
                circuitBreakerIdentifier,
                circuitBreakerPolicyConfig,
                new DefaultTripReasonAuthority()
            );
            new CircuitBreakerPolicyMetricsEventHandler(counterRegistry).RegisterEvents(circuitBreakerPolicy, _CategoryName, httpClientSettings.ClientName);
            AddHandlerAfter<RequestFailureThrowsHandler>(new CircuitBreakerHandler(circuitBreakerPolicy));
        }

        private const string _ClientCircuitBreakerPart = "MFDLabs.Discord.RbxUsers.Client.";
        private const string _CategoryName = "MFDLabs.Discord.RbxUsers.Client";
    }
}
