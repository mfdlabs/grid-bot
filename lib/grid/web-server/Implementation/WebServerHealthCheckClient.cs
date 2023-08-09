namespace MFDLabs.Grid;

using System;
using System.Net;
using System.Net.Http;

using Text.Extensions;
using Threading.Extensions;

/// <summary>
/// Implementation of <see cref="IHealthCheckClient"/>.
/// </summary>
public class WebServerHealthCheckClient : HealthCheckClientBase
{
    private readonly string _expectedText;

    /// <summary>
    /// Construct a new instance of <see cref="WebServerHealthCheckClient"/>.
    /// </summary>
    /// <param name="url">The url of the web server.</param>
    /// <param name="expectedHealthCheckText">The expected health check text.</param>
    /// <exception cref="ArgumentNullException"><paramref name="expectedHealthCheckText"/> cannot be null.</exception>
    public WebServerHealthCheckClient(
        string url,
        string expectedHealthCheckText
    )
        : base(url)
    {
        _expectedText = expectedHealthCheckText ?? throw new ArgumentNullException(nameof(expectedHealthCheckText));
    }

    /// <inheritdoc cref="IHealthCheckClient.HealthCheckPath"/>
    public override string HealthCheckPath { get; set; } = "/";

    /// <inheritdoc cref="HealthCheckClientBase.ValidateResponse(HttpResponseMessage)"/>
    protected override bool ValidateResponse(HttpResponseMessage response)
    {
        var responseText = response.Content.ReadAsStringAsync().Sync();

        return response.StatusCode == HttpStatusCode.OK &&
              !responseText.IsNullOrEmpty() &&
               responseText == _expectedText;
    }
}
