namespace Grid.Bot.Web.Middleware;

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Logging;

/// <summary>
/// Middleware for logging HTTP requests.
/// </summary>
public sealed class HttpServerRequestLoggingMiddleware : HttpServerMiddlewareBase
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    /// <summary>
    /// Construct a new instance of <see cref="HttpServerRequestLoggingMiddleware"/>
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/></param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="next"/> is <see langword="null"/>.
    /// - <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public HttpServerRequestLoggingMiddleware(RequestDelegate next, ILogger logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static string GetEndPointLogEntry(HttpContext context)
        => string.Format("\"{0} {1}\"", context.Request.Method, context.Request.Path);

    /// <summary>
    /// Invoke the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/></param>
    /// <returns>An awaitable <see cref="Task"/></returns>
    public async Task Invoke(HttpContext context)
    {
        var endpointLogEntry = GetEndPointLogEntry(context);
        if (string.Compare(context.Request.Method, "GET", StringComparison.CurrentCultureIgnoreCase) == 0)
            _logger.Debug("{0} request called: {1}", endpointLogEntry, context.Request.QueryString);
        else
            _logger.Debug("{0} request called", endpointLogEntry);

        var latency = Stopwatch.StartNew();

        try
        {
            await _next(context);

            _logger.Debug("{0} responded in {1} ms", endpointLogEntry, latency.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.Error("{0} failed in {1} ms: {2}", endpointLogEntry, latency.ElapsedMilliseconds, ex);

            throw;
        }
    }
}
