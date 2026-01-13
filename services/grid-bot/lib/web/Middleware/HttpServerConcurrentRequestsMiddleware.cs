namespace Grid.Bot.Web.Middleware;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Prometheus;

/// <summary>
/// Middleware for counting concurrent requests.
/// </summary>
public sealed class HttpServerConcurrentRequestsMiddleware
{
    private readonly RequestDelegate _next;
    
    private static readonly Gauge ConcurrentRequestsGauge = Metrics.CreateGauge("http_server_concurrent_requests_total", "The number of concurrent requests being processed by the server.");

    /// <summary>
    /// Construct a new instance of <see cref="HttpServerConcurrentRequestsMiddleware"/>
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> cannot be null.</exception>
    public HttpServerConcurrentRequestsMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/></param>
    /// <returns>An awaitable <see cref="Task"/></returns>
    public async Task Invoke(HttpContext context)
    {
        using (ConcurrentRequestsGauge.TrackInProgress()) 
            await _next(context);
    }
}
