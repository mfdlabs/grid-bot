using System;
using MFDLabs.Http.ServiceClient;

namespace MFDLabs.Analytics.Google.UniversalAnalytics.Client
{
    internal sealed class GaUniversalClientSettings : IServiceClientSettings
    {
        public string ClientName => "GAClient";
        public string UserAgent => "MFDLabs.Http.Client GAHttpClient";
        public string Endpoint { get; }
        public int MaxRedirects { get; }
        public int FailuresAllowedBeforeTrip { get; }
        public TimeSpan RetryInterval { get; }
        public TimeSpan RequestTimeout { get; }

        public GaUniversalClientSettings(GaUniversalClientConfig config)
        {
            Endpoint = config.Url;
            MaxRedirects = config.MaxRedirects;
            RequestTimeout = config.RequestTimeout;
            FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip;
            RetryInterval = config.CircuitBreakerRetryInterval;
        }
    }
}
