using System;

namespace MFDLabs.Google.Analytics.Client
{
    public class GAClientConfig
    {
        public string Url { get; }

        public string TrackerID { get; }

        public int MaxRedirects { get; }

        public TimeSpan RequestTimeout { get; }

        public int CircuitBreakerFailuresAllowedBeforeTrip { get; }

        public TimeSpan CircuitBreakerRetryInterval { get; }

        public GAClientConfig(string url, string trackerID, int maxRedirects, TimeSpan requestTimeout, int circuitBreakerFailuresAllowedBeforeTrip, TimeSpan circuitBreakerRetryInterval)
        {
            Url = url;
            TrackerID = trackerID;
            MaxRedirects = maxRedirects;
            RequestTimeout = requestTimeout;
            CircuitBreakerFailuresAllowedBeforeTrip = circuitBreakerFailuresAllowedBeforeTrip;
            CircuitBreakerRetryInterval = circuitBreakerRetryInterval;
        }
    }
}
