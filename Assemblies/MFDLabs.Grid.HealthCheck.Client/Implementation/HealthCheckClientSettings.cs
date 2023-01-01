namespace MFDLabs.Grid;

using System;

using Http.Client;
using Text.Extensions;
using Http.ServiceClient;
using Sentinels.CircuitBreakerPolicy;

/// <inheritdoc cref="IServiceClientSettings"/>
internal sealed class HealthCheckClientSettings : IServiceClientSettings
{
    private readonly string _clientName;
    private readonly string _clientUserAgent;

    /// <inheritdoc cref="IServiceClientSettings.ClientName"/>
    public string ClientName => _clientName;

    /// <inheritdoc cref="IHttpClientSettings.UserAgent"/>
    public string UserAgent => _clientUserAgent;

    /// <inheritdoc cref="IServiceClientSettings.Endpoint"/>
    public string Endpoint { get; }

    /// <inheritdoc cref="IHttpClientSettings.MaxRedirects"/>
    public int MaxRedirects { get; }

    /// <inheritdoc cref="IDefaultCircuitBreakerPolicyConfig.FailuresAllowedBeforeTrip"/>
    public int FailuresAllowedBeforeTrip { get; }

    /// <inheritdoc cref="IDefaultCircuitBreakerPolicyConfig.RetryInterval"/>
    public TimeSpan RetryInterval { get; }

    /// <inheritdoc cref="IHttpClientSettings.RequestTimeout"/>
    public TimeSpan RequestTimeout { get; }

    /// <summary>
    /// Construct a new instance of <see cref="HealthCheckClientSettings"/>
    /// </summary>
    /// <param name="configuration">The configuration to use</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="configuration"/> cannot be null.
    /// - configuration.ServiceName value has to be a non-empty string.
    /// - configuration.Url value has to be a non-empty string.
    /// </exception>
    public HealthCheckClientSettings(HealthCheckClientConfiguration configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        
        if (configuration.ServiceName.IsNullOrEmpty())
            throw new ArgumentException("configuration.ServiceName value has to be a non-empty string.", nameof(configuration));
        if (configuration.Url.IsNullOrEmpty())
            throw new ArgumentException("configuration.Url value has to be a non-empty string.", nameof(configuration));

        _clientName = string.Format("{0} Grid Health Check Client", configuration.ServiceName);
        _clientUserAgent = string.Format("mfdlabs/grid-health-check {0}", configuration.ServiceName.ToLower());

        Endpoint = configuration.Url;
        MaxRedirects = configuration.MaxRedirects;
        RequestTimeout = configuration.RequestTimeout;
        FailuresAllowedBeforeTrip = configuration.CircuitBreakerFailuresAllowedBeforeTrip;
        RetryInterval = configuration.CircuitBreakerRetryInterval;
    }
}
