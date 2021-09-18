﻿using MFDLabs.Http.Client;
using MFDLabs.Http.Client.Monitoring;
using MFDLabs.Instrumentation;
using MFDLabs.RequestContext;
using MFDLabs.Text.Extensions;
using System;
using System.Net;

namespace MFDLabs.Http.ServiceClient
{
    public class HttpClientBuilder : Client.HttpClientBuilder
    {
        public Func<bool> ApiKeyViaHeaderEnabled { get; set; }

        public Func<ClientCircuitBreakerType> GetClientCircuitBreakerType { get; set; }

        public HttpClientBuilder(IServiceClientSettings serviceClientSettings, ICounterRegistry counterRegistry, Func<string> apiKeyGetter, Func<bool> apiKeyViaHeaderEnabled = null, CookieContainer cookieContainer = null, IRequestContextLoader requestContextLoader = null, IHttpMessageHandlerBuilder httpMessageHandlerBuilder = null) : base(cookieContainer, serviceClientSettings, httpMessageHandlerBuilder)
        {
            if (serviceClientSettings == null)
            {
                throw new ArgumentNullException("serviceClientSettings");
            }
            if (counterRegistry == null)
            {
                throw new ArgumentNullException("counterRegistry");
            }
            if (apiKeyGetter == null)
            {
                throw new ArgumentNullException("apiKeyGetter");
            }
            if (serviceClientSettings.ClientName.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("serviceClientSettings.ClientName value has to be a non-empty string.", "serviceClientSettings");
            }
            ApiKeyViaHeaderEnabled = apiKeyViaHeaderEnabled;
            AppendHandler(new MachineIdHandler());
            AppendHandler(new TracingHandler());
            AppendHandler(new OperationErrorHandler());
            AppendHandler(new ApiKeyHandler(apiKeyGetter, IsApiKeyViaHeaderEnabled));
            if (requestContextLoader != null)
            {
                AppendHandler(new RequestContextHandler(requestContextLoader));
            }
            AddHandlerBefore<SendHttpRequestHandler>(new HttpRequestMetricsHandler(counterRegistry, "MFDLabs.ApiClient", serviceClientSettings.ClientName));
            var circuitBreakerProvider = new DynamicHttpClientCircuitBreakerProvider(serviceClientSettings, GetCircuitBreakerType, "MFDLabs.GuardedApiClientV2." + serviceClientSettings.ClientName);
            circuitBreakerProvider.CircuitBreakerPolicyCreated += (policy) =>
            {
                new CircuitBreakerPolicyMetricsEventHandler(counterRegistry).RegisterEvents(policy, _CircuitBreakerPerformanceMetricsCategory, serviceClientSettings.ClientName);
            };
            AddHandlerAfter<RequestFailureThrowsHandler>(new CircuitBreakerHandler(circuitBreakerProvider));
        }

        private bool IsApiKeyViaHeaderEnabled()
        {
            return ApiKeyViaHeaderEnabled != null && ApiKeyViaHeaderEnabled();
        }

        private ClientCircuitBreakerType GetCircuitBreakerType()
        {
            if (GetClientCircuitBreakerType == null)
            {
                return ClientCircuitBreakerType.WholeClient;
            }
            return GetClientCircuitBreakerType();
        }

        private const string _CircuitBreakerPerformanceMetricsCategory = "MFDLabs.GuardedApiClientV2";
    }
}
