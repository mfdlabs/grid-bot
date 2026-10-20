namespace Grid.Bot.Web.Middleware;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Prometheus;
using Grid.Bot.Metrics;

/// <summary>
/// Middleware for counting http server requests.
/// </summary>
public sealed class HttpServerRequestCountMiddleware : HttpServerMiddlewareBase
{
    private readonly RequestDelegate _next;
    private readonly Counter _HttpRequestCounter = GridBotMetrics.HttpServer.Requests;

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
        var endpoint = GetEndpointLabel(context);

        _HttpRequestCounter.WithLabels(context.Request.Method, endpoint).Inc();

        await _next(context);
    }

}
