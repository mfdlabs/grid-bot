namespace MFDLabs.Grid;

using System;

/// <summary>
/// The HTTP client configuration for a health-check client.
/// </summary>
public class HealthCheckClientConfiguration
{
    /// <summary>
    /// Gets the name of the service the health check is being called on.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets the health check server's url.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// Gets the maximum amount of redirects the target server can perform.
    /// </summary>
    public int MaxRedirects { get; }

    /// <summary>
    /// Gets the time before a request is considered timed-out.
    /// </summary>
    public TimeSpan RequestTimeout { get; }

    /// <summary>
    /// Gets the max amount of failures before the HTTP client's circuit breaker is tripped.
    /// </summary>
    public int CircuitBreakerFailuresAllowedBeforeTrip { get; }

    /// <summary>
    /// Gets the retry interval for the HTTP client's circuit breaker.
    /// </summary>
    public TimeSpan CircuitBreakerRetryInterval { get; }

    /// <summary>
    /// Construct a new instance of <see cref="HealthCheckClientConfiguration"/>
    /// </summary>
    /// <param name="serviceName">The name of the service the health check is being called on.</param>
    /// <param name="url">The health check server's url.</param>
    /// <param name="maxRedirects">The maximum amount of redirects the target server can perform.</param>
    /// <param name="requestTimeout">The time before a request is considered timed-out.</param>
    /// <param name="circuitBreakerFailuresAllowedBeforeTrip">The max amount of failures before the HTTP client's circuit breaker is tripped.</param>
    /// <param name="circuitBreakerRetryInterval">The retry interval for the HTTP client's circuit breaker.</param>
    public HealthCheckClientConfiguration(
        string serviceName,
        string url,
        int maxRedirects,
        TimeSpan requestTimeout,
        int circuitBreakerFailuresAllowedBeforeTrip,
        TimeSpan circuitBreakerRetryInterval
    )
    {
        ServiceName = serviceName;
        Url = url;
        MaxRedirects = maxRedirects;
        RequestTimeout = requestTimeout;
        CircuitBreakerFailuresAllowedBeforeTrip = circuitBreakerFailuresAllowedBeforeTrip;
        CircuitBreakerRetryInterval = circuitBreakerRetryInterval;
    }
}
