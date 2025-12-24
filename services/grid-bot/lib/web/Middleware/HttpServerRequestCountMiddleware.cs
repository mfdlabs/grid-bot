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
    private readonly Counter _HttpRequestCounter = Metrics.CreateCounter(
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
        var endpoint = controller != _UnknownRouteLabelValue && action != _UnknownRouteLabelValue ?
            string.Format("{0}.{1}", controller, action)
            : context.Request.Path.Value ?? _UnknownRouteLabelValue;

        _HttpRequestCounter.WithLabels(context.Request.Method, endpoint).Inc();

        await _next(context);
    }

}
