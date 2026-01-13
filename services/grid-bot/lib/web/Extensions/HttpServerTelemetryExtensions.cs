namespace Grid.Bot.Web.Middleware;

using System;

using Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding telemetry to the HTTP server.
/// </summary>
public static class HttpServerTelemetryExtensions
{
    /// <param name="app">The <see cref="IApplicationBuilder"/></param>
    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Adds telemetry to the HTTP server.
        /// </summary>
        /// <returns>The <see cref="IApplicationBuilder"/></returns>
        public IApplicationBuilder UseTelemetry()
        {
            ArgumentNullException.ThrowIfNull(app);

            app.UseMiddleware<HttpServerConcurrentRequestsMiddleware>();
            app.UseMiddleware<HttpServerRequestCountMiddleware>();
            app.UseMiddleware<HttpServerResponseMiddleware>();

            return app;
        }
    }
}
