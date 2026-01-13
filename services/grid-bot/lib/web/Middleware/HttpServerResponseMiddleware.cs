namespace Grid.Bot.Web.Middleware;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Prometheus;

/// <summary>
/// Middleware to handle the response
/// </summary>
/// <summary>
/// Middleware for counting and recording http response metrics.
/// </summary>
public sealed class HttpServerResponseMiddleware : HttpServerMiddlewareBase
{
    private readonly RequestDelegate _next;

    private static readonly Counter HttpResponseCounter = Metrics.CreateCounter(
        "http_server_response_total",
        "Total number of http responses",
        "method",
        "endpoint",
        "status_code"
    );
    private static readonly Histogram RequestDurationHistogram= Metrics.CreateHistogram(
        "http_server_request_duration_seconds",
        "Duration in seconds each request takes",
        "method",
        "endpoint"
    );

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpServerResponseMiddleware" /> class.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate" />.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="next" /> cannot be null.
    /// </exception>
    public HttpServerResponseMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Invokes the middleware
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" />.</param>
    /// <returns>A <see cref="Task" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> cannot be null.</exception>
    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var latencyStopwatch = Stopwatch.StartNew();
        var (controller, action) = GetControllerAndAction(context);
        var endpoint = controller != UnknownRouteLabelValue && action != UnknownRouteLabelValue
            ? $"{controller}.{action}"
            : context.Request.Path.Value ?? UnknownRouteLabelValue;

        try
        {
            await _next(context).ConfigureAwait(false);

            context.Response.OnCompleted(() =>
            {
                latencyStopwatch.Stop();

                if (context.Response.StatusCode is >= 200 and < 300)
                    RequestDurationHistogram.WithLabels(context.Request.Method, endpoint).Observe(latencyStopwatch.Elapsed.TotalSeconds);

                HttpResponseCounter.WithLabels(context.Request.Method, endpoint, context.Response.StatusCode.ToString()).Inc();

                return Task.CompletedTask;
            });
        }
        catch (Exception)
        {
            HttpResponseCounter.WithLabels(context.Request.Method, endpoint, context.Response.StatusCode.ToString()).Inc();

            latencyStopwatch.Stop();

            throw;
        }
    }
}
