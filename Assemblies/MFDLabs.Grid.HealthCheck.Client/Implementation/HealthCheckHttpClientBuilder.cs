namespace MFDLabs.Grid;

using System;

using Http;
using Pipeline;
using Http.Client;
using Text.Extensions;
using Instrumentation;
using Http.ServiceClient;
using Http.Client.Monitoring;
using Sentinels.CircuitBreakerPolicy;

using HttpClientBuilder = Http.Client.HttpClientBuilder;

/// <summary>
/// The builder for the <see cref="IHttpClient"/> used by the <see cref="HealthCheckClientBase"/>
/// </summary>
internal sealed class HealthCheckHttpClientBuilder : HttpClientBuilder
{
    private const string CategoryName = "Grid Health Check Client";

    /// <summary>
    /// Construct a new instance of <see cref="HealthCheckHttpClientBuilder"/>
    /// </summary>
    /// <param name="counterRegistry">The counter registry to use</param>
    /// <param name="httpClientSettings">The settings to use</param>
    /// <param name="configuration">The configuration to use</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="counterRegistry"/> cannot be null.
    /// - <paramref name="httpClientSettings"/> cannot be null.
    /// - <paramref name="configuration"/> cannot be null.
    /// </exception>
    /// <exception cref="ArgumentException">httpClientSettings.ClientName value has to be a non-empty string.</exception>
    public HealthCheckHttpClientBuilder(
        ICounterRegistry counterRegistry,
        HealthCheckClientSettings httpClientSettings,
        HealthCheckClientConfiguration configuration
    )
            : base(null, httpClientSettings)
    {
        if (counterRegistry == null) throw new ArgumentNullException(nameof(counterRegistry));
        if (httpClientSettings == null) throw new ArgumentNullException(nameof(httpClientSettings));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        
        if (httpClientSettings.ClientName.IsNullOrWhiteSpace())
            throw new ArgumentException("httpClientSettings.ClientName value has to be a non-empty string.", nameof(httpClientSettings));

        AppendHandler(new OperationErrorHandler());
        AddHandlerBefore<SendHttpRequestHandler>(
            new HttpRequestMetricsHandler(counterRegistry, CategoryName, httpClientSettings.ClientName)
        );

        var circuitBreakerPolicy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(
            httpClientSettings.ClientName,
            new DefaultCircuitBreakerPolicyConfig
            {
                FailuresAllowedBeforeTrip = configuration.CircuitBreakerFailuresAllowedBeforeTrip,
                RetryInterval = configuration.CircuitBreakerRetryInterval
            },
            new DefaultTripReasonAuthority()
        );

        new CircuitBreakerPolicyMetricsEventHandler(counterRegistry)
            .RegisterEvents(circuitBreakerPolicy, CategoryName, httpClientSettings.ClientName);
        
        AddHandlerAfter<RequestFailureThrowsHandler>(new CircuitBreakerHandler(circuitBreakerPolicy));
    }
}
