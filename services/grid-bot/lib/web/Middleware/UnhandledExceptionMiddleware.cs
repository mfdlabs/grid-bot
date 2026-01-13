namespace Grid.Bot.Web.Middleware;

using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Logging;

/// <summary>
/// Middleware for logging unhandled exceptions and responding with <see cref="HttpStatusCode.InternalServerError"/>.
/// </summary>
public class UnhandledExceptionMiddleware
{
    private readonly RequestDelegate _nextHandler;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new <see cref="UnhandledExceptionMiddleware"/>.
    /// </summary>
    /// <param name="nextHandler">A delegate for triggering the next handler.</param>
    /// <param name="logger">An <see cref="ILogger"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="nextHandler"/>
    /// - <paramref name="logger"/>
    /// </exception>
    public UnhandledExceptionMiddleware(RequestDelegate nextHandler, ILogger logger)
    {
        _nextHandler = nextHandler ?? throw new ArgumentNullException(nameof(nextHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The method to invoke the handler.
    /// </summary>
    /// <param name="context">An <see cref="HttpContext"/>.</param>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _nextHandler(context);
        }
        catch (Exception ex)
        {
            _logger.Error(ex);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(ex.ToString());
        }
    }
}
