namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Http;
using Http.Client;
using Instrumentation;
using MFDLabs.Sentinels;
using Text.Extensions;

using HttpMethod = Http.HttpMethod;

/// <inheritdoc cref="IHealthCheckClient"/>
public abstract class HealthCheckClientBase : IHealthCheckClient
{
    private sealed class ExposeHttpResponseRequestSender
    {
        private readonly IHttpClient _client;
        private readonly IHttpRequestBuilderSettings _builderSettings;

        internal ExposeHttpResponseRequestSender(IHttpClient httpClient, IHttpRequestBuilderSettings settings)
        {
            _client = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _builderSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public IHttpResponse Get(string path)
        {
            ValidatePath(path);

            return _client.Send(new HttpRequest(HttpMethod.Get, CreateUriBuilder(path).Uri));
        }

        public async Task<IHttpResponse> GetAsync(string path, CancellationToken cancellationToken)
        {
            ValidatePath(path);

            return await _client.SendAsync(new HttpRequest(HttpMethod.Get, CreateUriBuilder(path).Uri), cancellationToken);
        }

        private UriBuilder CreateUriBuilder(string path) =>
            new(_builderSettings.Endpoint)
            {
                Path = path
            };

        private static void ValidatePath(string path)
        {
            if (path.IsNullOrWhiteSpace())
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(path));
        }
    }

    private readonly ExposeHttpResponseRequestSender _requestSender;

    /// <summary>
    /// Construct a new instance of <see cref="HealthCheckClientBase"/>
    /// </summary>
    /// <param name="counterRegistry">The counter registry to use</param>
    /// <param name="configuration">The configuration to use</param>
    protected HealthCheckClientBase(ICounterRegistry counterRegistry, HealthCheckClientConfiguration configuration)
    {
        var settings = new HealthCheckClientSettings(configuration);

        _requestSender = new ExposeHttpResponseRequestSender(
            new HealthCheckHttpClientBuilder(
                counterRegistry,
                settings,
                configuration
            ).Build(),
            new HttpRequestBuilderSettings(settings.Endpoint)
        );
    }

    /// <inheritdoc cref="IHealthCheckClient.HealthCheckPath"/>
    public virtual string HealthCheckPath { get; set; } = "/health";

    /// <summary>
    /// Validates the response from the health check endpoint.
    /// </summary>
    /// <param name="response">The response to validate</param>
    /// <returns>True if the response is valid</returns>
    protected abstract bool ValidateResponse(IHttpResponse response);

    /// <inheritdoc cref="IHealthCheckClient.CheckHealth"/>
    public virtual HealthCheckStatus CheckHealth()
    {
        try
        {
            var isValid = ValidateResponse(_requestSender.Get(HealthCheckPath));

            return isValid ? HealthCheckStatus.Success : HealthCheckStatus.Failure;
        }
        catch (HttpException ex) when (ex.InnerException is WebException webEx && webEx.Status == WebExceptionStatus.Timeout)
        {
            return HealthCheckStatus.Timeout;
        }
        catch (CircuitBreakerException) { return HealthCheckStatus.CircuitBreakerTripped; }
        catch { return HealthCheckStatus.UnknownError; }
    }

    /// <inheritdoc cref="IHealthCheckClient.CheckHealthAsync"/>
    public virtual async Task<HealthCheckStatus> CheckHealthAsync(CancellationToken? cancellationToken = null)
    {
        try
        {
            var isValid = ValidateResponse(await _requestSender.GetAsync(HealthCheckPath, cancellationToken ?? CancellationToken.None));

            return isValid ? HealthCheckStatus.Success : HealthCheckStatus.Failure;
        }
        catch (Exception ex) when (ex.InnerException is WebException webEx && webEx.Status == WebExceptionStatus.Timeout)
        {
            return HealthCheckStatus.Timeout;
        }
        catch (TaskCanceledException) { return HealthCheckStatus.Cancelled; }
        catch (CircuitBreakerException) { return HealthCheckStatus.CircuitBreakerTripped; }
        catch { return HealthCheckStatus.UnknownError; }
    }
}
