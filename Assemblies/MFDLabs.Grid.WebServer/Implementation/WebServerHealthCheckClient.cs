namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Text;

using Http;
using Instrumentation;
using Text.Extensions;

/// <summary>
/// Implementation of <see cref="IHealthCheckClient"/>.
/// </summary>
public class WebServerHealthCheckClient : HealthCheckClientBase
{
    private readonly string _expectedText;

    /// <summary>
    /// Construct a new instance of <see cref="WebServerHealthCheckClient"/>.
    /// </summary>
    /// <param name="counterRegistry">The counter registry to use.</param>
    /// <param name="url">The url of the web server.</param>
    /// <param name="expectedHealthCheckText">The expected health check text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="expectedHealthCheckText"/> cannot be null.</exception>
    public WebServerHealthCheckClient(
        ICounterRegistry counterRegistry,
        string url,
        string expectedHealthCheckText
    )
        : base(
            counterRegistry,
            new(
                "grid-service-websrv", 
                url,
                global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckClientMaxRedirects,
                global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckClientRequestTimeout,
                global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckClientAllowedFailuresBeforeTrip,
                global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckClientRetryInterval
            )
        )
    {
        _expectedText = expectedHealthCheckText ?? throw new ArgumentNullException(nameof(expectedHealthCheckText));
    }

    /// <inheritdoc cref="IHealthCheckClient.HealthCheckPath"/>
    public override string HealthCheckPath { get; set; } = "/";

    /// <inheritdoc cref="HealthCheckClientBase.ValidateResponse(IHttpResponse)"/>
    protected override bool ValidateResponse(IHttpResponse response)
    {
        var responseText = Encoding.ASCII.GetString(response.Body);

        return response.StatusCode == HttpStatusCode.OK &&
              !responseText.IsNullOrEmpty() && 
               responseText == _expectedText;
    }
}
