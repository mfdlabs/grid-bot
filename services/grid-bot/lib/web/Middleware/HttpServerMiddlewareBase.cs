namespace Grid.Bot.Web.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Base middleware for http servers.
/// </summary>
public abstract class HttpServerMiddlewareBase
{
    /// <summary>
    /// The endpoint label value for requests that did not match a route.
    /// </summary>
    protected const string _UnknownRouteLabelValue = "unknown";

    /// <summary>
    /// Gets the endpoint label value for metrics.
    /// </summary>
    /// <remarks>
    /// This is the route pattern, such as <c>/v1/avatar-fetch</c>, never the raw request path, so the number
    /// of values is bounded by the number of routes.
    /// </remarks>
    /// <param name="context">The <see cref="HttpContext"/></param>
    /// <returns>The route pattern, or <see cref="_UnknownRouteLabelValue"/> if no route matched.</returns>
    protected static string GetEndpointLabel(HttpContext context)
        => (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? _UnknownRouteLabelValue;
}
