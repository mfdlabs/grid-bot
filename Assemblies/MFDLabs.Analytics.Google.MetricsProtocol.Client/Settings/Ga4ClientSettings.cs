namespace MFDLabs.Analytics.Google.MetricsProtocol.Client;

using System;

using Http.ServiceClient;


internal sealed class Ga4ClientSettings : IServiceClientSettings
{
    public string ClientName => "GAClient";
    public string UserAgent => "MFDLabs.Http.Client GA4HttpClient";
    public string Endpoint { get; }
    public int MaxRedirects { get; }
    public int FailuresAllowedBeforeTrip { get; }
    public TimeSpan RetryInterval { get; }
    public TimeSpan RequestTimeout { get; }

    public Ga4ClientSettings(Ga4ClientConfig config)
    {
        Endpoint = config.Url;
        MaxRedirects = config.MaxRedirects;
        RequestTimeout = config.RequestTimeout;
        FailuresAllowedBeforeTrip = config.CircuitBreakerFailuresAllowedBeforeTrip;
        RetryInterval = config.CircuitBreakerRetryInterval;
    }
}
