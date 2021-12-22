using System;

namespace MFDLabs.Discord.RbxUsers.Client
{
    public class RbxDiscordUsersClientConfig
    {
        public string Url { get; }
        public int MaxRedirects { get; }
        public TimeSpan RequestTimeout { get; }
        public int CircuitBreakerFailuresAllowedBeforeTrip { get; }
        public TimeSpan CircuitBreakerRetryInterval { get; }

        public RbxDiscordUsersClientConfig(string url,
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
