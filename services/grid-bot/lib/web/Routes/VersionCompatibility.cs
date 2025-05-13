namespace Grid.Bot.Web.Routes;

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Routes for the versioncompatibility API.
/// </summary>
public class VersionCompatibility
{
    /// <summary>
    /// Get allowed MD5 hashes, for now this responds with an empty response.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" /></param>
    public async Task GetAllowedMd5Hashes(HttpContext context)
        => await context.Response.WriteAsJsonAsync(new { data = Array.Empty<object>() });
}
