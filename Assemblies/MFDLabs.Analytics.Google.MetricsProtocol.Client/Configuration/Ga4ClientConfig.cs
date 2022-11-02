namespace MFDLabs.Analytics.Google.MetricsProtocol.Client;

using System;


public class Ga4ClientConfig
{
    #region Defaults

    public string Url { get; }
    public int MaxRedirects { get; }
    public TimeSpan RequestTimeout { get; }
    public int CircuitBreakerFailuresAllowedBeforeTrip { get; }
    public TimeSpan CircuitBreakerRetryInterval { get; }

    #endregion Defaults


    public string MetricsId { get; }
    public string ApiSecret { get; }
    public bool ServerSideValidationEnabled { get; }

    public Ga4ClientConfig(string url,
        string metricsId,
        string apiSecret,
        bool serverSideValidationEnabled,
        int maxRedirects,
        TimeSpan requestTimeout,
        int circuitBreakerFailuresAllowedBeforeTrip,
        TimeSpan circuitBreakerRetryInterval
    )
    {
        Url = url;
        MetricsId = metricsId;
        ApiSecret = apiSecret;
        ServerSideValidationEnabled = serverSideValidationEnabled;
        MaxRedirects = maxRedirects;
        RequestTimeout = requestTimeout;
        CircuitBreakerFailuresAllowedBeforeTrip = circuitBreakerFailuresAllowedBeforeTrip;
        CircuitBreakerRetryInterval = circuitBreakerRetryInterval;
    }
}
