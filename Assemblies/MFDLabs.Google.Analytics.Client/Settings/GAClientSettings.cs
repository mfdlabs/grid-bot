using System;
using MFDLabs.Http.Client;
using MFDLabs.Http.ServiceClient;
using MFDLabs.Sentinels.CircuitBreakerPolicy;

namespace MFDLabs.Google.Analytics.Client
{
    internal sealed class GAClientSettings : IServiceClientSettings, IHttpClientSettings, IDefaultCircuitBreakerPolicyConfig
    {
        public string ClientName
        {
            get
            {
                return "GAClient";
            }
        }

        public string UserAgent
        {
            get
            {
                return "MFDLabs.Http.Client GAHttpClient";
            }
        }

        public string Endpoint { get; }

        public int MaxRedirects { get; }

        public int FailuresAllowedBeforeTrip { get; }

        public TimeSpan RetryInterval { get; }

        public TimeSpan RequestTimeout { get; }

        public GAClientSettings(GAClientConfig config)
        {
            Endpoint = config.Url;
            MaxRedirects = config.MaxRedirects;
            RequestTimeout = config.RequestTimeout;
            FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip;
            RetryInterval = config.CircuitBreakerRetryInterval;
        }
    }
}
