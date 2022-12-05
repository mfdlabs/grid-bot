
namespace MFDLabs.Analytics.Google.MetricsProtocol.Client;

using System;

using Http;
using Pipeline;
using Http.Client;
using Instrumentation;
using Text.Extensions;
using Http.ServiceClient;
using Http.Client.Monitoring;
using Sentinels.CircuitBreakerPolicy;

using HttpClientBuilder = Http.Client.HttpClientBuilder;


internal sealed class Ga4ValidatorHttpClientBuilder : HttpClientBuilder
{
    public Ga4ValidatorHttpClientBuilder(
        ICounterRegistry counterRegistry,
        Ga4ClientSettings httpClientSettings,
        Ga4ClientConfig config
    )
        : base(null, httpClientSettings)
    {
        if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
        if (httpClientSettings == null) throw new ArgumentNullException(nameof(httpClientSettings));
        if (config == null) throw new ArgumentNullException(nameof(config));
        if (httpClientSettings.ClientName.IsNullOrWhiteSpace())
            throw new ArgumentException("httpClientSettings.ClientName value has to be a non-empty string.", nameof(httpClientSettings));

        AppendHandler(new OperationErrorHandler());
        AddHandlerBefore<SendHttpRequestHandler>(new HttpRequestMetricsHandler(counterRegistry, CategoryName, httpClientSettings.ClientName + "Validator"));

        var circuitBreakerPolicy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(
            ClientCircuitBreakerPart + httpClientSettings.ClientName + "Validator",
            new DefaultCircuitBreakerPolicyConfig
            {
                FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip,
                RetryInterval = config.CircuitBreakerRetryInterval
            },
            new DefaultTripReasonAuthority()
        );

        new CircuitBreakerPolicyMetricsEventHandler(counterRegistry).RegisterEvents(circuitBreakerPolicy, CategoryName, httpClientSettings.ClientName + "Validator");
        AddHandlerAfter<RequestFailureThrowsHandler>(new CircuitBreakerHandler(circuitBreakerPolicy));
    }

    private const string ClientCircuitBreakerPart = "MFDLabs.Analytics.Google.MetricsProtocolValidator.Client.";
    private const string CategoryName = "MFDLabs.Analytics.Google.MetricsProtocolValidator.Client";
}
