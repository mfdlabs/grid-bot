using System;

namespace MFDLabs.Users.Client
{
    public class UsersClientConfig
    {
        public string Url { get; }
        public int MaxRedirects { get; }
        public TimeSpan RequestTimeout { get; }
        public int CircuitBreakerFailuresAllowedBeforeTrip { get; }
        public TimeSpan CircuitBreakerRetryInterval { get; }

        public UsersClientConfig(string url,
            int maxRedirects,
            TimeSpan requestTimeout,
            int circuitBreakerFailuresAllowedBeforeTrip,
            TimeSpan circuitBreakerRetryInterval)
        {
            Url = url;
            MaxRedirects = maxRedirects;
            RequestTimeout = requestTimeout;
            CircuitBreakerFailuresAllowedBeforeTrip = circuitBreakerFailuresAllowedBeforeTrip;
            CircuitBreakerRetryInterval = circuitBreakerRetryInterval;
        }
    }
}
