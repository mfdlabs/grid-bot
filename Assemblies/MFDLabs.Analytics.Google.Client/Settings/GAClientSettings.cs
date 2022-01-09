using System;
using MFDLabs.Http.ServiceClient;

namespace MFDLabs.Analytics.Google.Client
{
    internal sealed class GaClientSettings : IServiceClientSettings
    {
        public string ClientName => "GAClient";
        public string UserAgent => "MFDLabs.Http.Client GAHttpClient";
        public string Endpoint { get; }
        public int MaxRedirects { get; }
        public int FailuresAllowedBeforeTrip { get; }
        public TimeSpan RetryInterval { get; }
        public TimeSpan RequestTimeout { get; }

        public GaClientSettings(GaClientConfig config)
        {
            Endpoint = config.Url;
            MaxRedirects = config.MaxRedirects;
            RequestTimeout = config.RequestTimeout;
            FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip;
            RetryInterval = config.CircuitBreakerRetryInterval;
        }
    }
}
