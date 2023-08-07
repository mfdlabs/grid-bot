namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using MFDLabs.Sentinels;
using MFDLabs.Threading.Extensions;

/// <inheritdoc cref="IHealthCheckClient"/>
public abstract class HealthCheckClientBase : IHealthCheckClient
{
    private readonly string _baseUrl;

    /// <summary>
    /// Construct a new instance of <see cref="HealthCheckClientBase"/>
    /// </summary>
    /// <param name="baseUrl">The baseUrl</param>
    protected HealthCheckClientBase(string baseUrl)
    {
        if (string.IsNullOrEmpty(baseUrl)) throw new ArgumentException("baseUrl cannot be null or empty!", nameof(baseUrl));

        _baseUrl = baseUrl;
    }

    /// <inheritdoc cref="IHealthCheckClient.HealthCheckPath"/>
    public virtual string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Validates the response from the health check endpoint.
    /// </summary>
    /// <param name="response">The response to validate</param>
    /// <returns>True if the response is valid</returns>
    protected abstract bool ValidateResponse(HttpResponseMessage response);

    /// <inheritdoc cref="IHealthCheckClient.CheckHealth"/>
    public virtual HealthCheckStatus CheckHealth()
    {
        try
        {
            using var httpClient = new HttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(_baseUrl));

            var isValid = ValidateResponse(httpClient.SendAsync(httpRequest).Sync());

            return isValid ? HealthCheckStatus.Success : HealthCheckStatus.Failure;
        }
        catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
        {
            return HealthCheckStatus.Timeout;
        }
        catch { return HealthCheckStatus.UnknownError; }
    }

    /// <inheritdoc cref="IHealthCheckClient.CheckHealthAsync"/>
    public virtual async Task<HealthCheckStatus> CheckHealthAsync(CancellationToken? cancellationToken = null)
    {
        try
        {
            using var httpClient = new HttpClient();
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(_baseUrl));

            var isValid = ValidateResponse(await httpClient.SendAsync(httpRequest, cancellationToken ?? CancellationToken.None));

            return isValid ? HealthCheckStatus.Success : HealthCheckStatus.Failure;
        }
        catch (WebException ex) when (ex.Status == WebExceptionStatus.Timeout)
        {
            return HealthCheckStatus.Timeout;
        }
        catch (TaskCanceledException) { return HealthCheckStatus.Cancelled; }
        catch (CircuitBreakerException) { return HealthCheckStatus.CircuitBreakerTripped; }
        catch { return HealthCheckStatus.UnknownError; }
    }
}
