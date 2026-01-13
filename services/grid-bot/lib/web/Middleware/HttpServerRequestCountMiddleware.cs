namespace Grid.Bot.Web.Middleware;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Prometheus;

/// <summary>
/// Middleware for counting http server requests.
/// </summary>
public sealed class HttpServerRequestCountMiddleware : HttpServerMiddlewareBase
{
    private readonly RequestDelegate _next;
    private static readonly Counter HttpRequestCounter = Metrics.CreateCounter(
        "http_server_requests_total", 
        "Total number of http requests", 
        "method", 
        "endpoint"
    );

    /// <summary>
    /// Construct a new instance of <see cref="HttpServerRequestCountMiddleware"/>
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> cannot be null.</exception>
    public HttpServerRequestCountMiddleware(RequestDelegate next)
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
        var (controller, action) = GetControllerAndAction(context);
        var endpoint = controller != UnknownRouteLabelValue && action != UnknownRouteLabelValue ? 
            $"{controller}.{action}"
            : context.Request.Path.Value ?? UnknownRouteLabelValue;

        HttpRequestCounter.WithLabels(context.Request.Method, endpoint).Inc();

        await _next(context);
    }

}
