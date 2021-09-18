﻿using MFDLabs.Http;
using MFDLabs.Http.Client;
using MFDLabs.Http.Client.Monitoring;
using MFDLabs.Http.ServiceClient;
using MFDLabs.Instrumentation;
using MFDLabs.Pipeline;
using MFDLabs.Sentinels.CircuitBreakerPolicy;
using MFDLabs.Text.Extensions;
using System;
using HttpClientBuilder = MFDLabs.Http.Client.HttpClientBuilder;

namespace MFDLabs.Users.Client
{
    internal sealed class UsersHttpClientBuilder : HttpClientBuilder
    {
        public UsersHttpClientBuilder(ICounterRegistry counterRegistry, UsersClientSettings httpClientSettings, UsersClientConfig config) : base(null, httpClientSettings, null)
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
            DefaultCircuitBreakerPolicyConfig circuitBreakerPolicyConfig = new DefaultCircuitBreakerPolicyConfig
            {
                FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip,
                RetryInterval = config.CircuitBreakerRetryInterval
            };
            DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>> circuitBreakerPolicy = new DefaultCircuitBreakerPolicy<IExecutionContext<IHttpRequest, IHttpResponse>>(circuitBreakerIdentifier, circuitBreakerPolicyConfig, new DefaultTripReasonAuthority());
            new CircuitBreakerPolicyMetricsEventHandler(counterRegistry).RegisterEvents(circuitBreakerPolicy, _CategoryName, httpClientSettings.ClientName);
            AddHandlerAfter<RequestFailureThrowsHandler>(new CircuitBreakerHandler(circuitBreakerPolicy));
        }

        private const string _ClientCircuitBreakerPart = "MFDLabs.Users.Client.";
        private const string _CategoryName = "MFDLabs.Users.Client";
    }
}
