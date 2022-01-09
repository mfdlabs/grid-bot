using System;

namespace MFDLabs.Analytics.Google.Client
{
    public class GaClientConfig
    {
        public string Url { get; }
        public string TrackerId { get; }
        public int MaxRedirects { get; }
        public TimeSpan RequestTimeout { get; }
        public int CircuitBreakerFailuresAllowedBeforeTrip { get; }
        public TimeSpan CircuitBreakerRetryInterval { get; }

        public GaClientConfig(string url,
            string trackerId,
            int maxRedirects,
            TimeSpan requestTimeout,
            int circuitBreakerFailuresAllowedBeforeTrip,
            TimeSpan circuitBreakerRetryInterval)
        {
            Url = url;
            TrackerId = trackerId;
            MaxRedirects = maxRedirects;
            RequestTimeout = requestTimeout;
            CircuitBreakerFailuresAllowedBeforeTrip = circuitBreakerFailuresAllowedBeforeTrip;
            CircuitBreakerRetryInterval = circuitBreakerRetryInterval;
        }
    }
}
