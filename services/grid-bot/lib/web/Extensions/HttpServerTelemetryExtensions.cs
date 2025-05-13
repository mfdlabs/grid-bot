namespace Grid.Bot.Web.Middleware;

using System;

using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding telemetry to the HTTP server.
/// </summary>
public static class HttpServerTelemetryExtensions
{
    /// <summary>
    /// Adds telemetry to the HTTP server.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/></param>
    /// <returns>The <see cref="IApplicationBuilder"/></returns>
    public static IApplicationBuilder UseTelemetry(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

		app.UseMiddleware<HttpServerConcurrentRequestsMiddleware>();
		app.UseMiddleware<HttpServerRequestCountMiddleware>();
		app.UseMiddleware<HttpServerResponseMiddleware>();

        return app;
    }
}
