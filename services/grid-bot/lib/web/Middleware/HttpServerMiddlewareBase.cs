namespace Grid.Bot.Web.Middleware;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

/// <summary>
/// Base middleware for http servers.
/// </summary>
public abstract class HttpServerMiddlewareBase
{
    /// <summary>
    /// Unknown route value.
    /// </summary>
    protected const string _UnknownRouteLabelValue = "Unknown";

    /// <summary>
    /// Get endpoint label value for metrics.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/></param>
    /// <returns>The endpoint label value or <see cref="_UnknownRouteLabelValue"/></returns>
    protected static (string controller, string action) GetControllerAndAction(HttpContext context)
    {
        var routeData = context.GetRouteData();
        var action = routeData?.Values["action"] as string ?? string.Empty;
        var controller = routeData?.Values["controller"] as string ?? string.Empty;

        if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(controller))
            return (_UnknownRouteLabelValue, _UnknownRouteLabelValue);

        return (controller, action);
    }
}
