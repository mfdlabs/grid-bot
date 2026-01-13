namespace Grid.Bot.Web.Extensions;

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Extensions for <see cref="HttpContext"/>, <see cref="HttpRequest"/> and <see cref="HttpResponse"/>
/// </summary>
public static class HttpContextExtensions
{
    private const string ApiKeyHeaderName = "x-api-key";

    /// <summary>
    /// Writes an error to the response.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponse"/></param>
    /// <param name="error">The error message</param>
    public static async Task WriteRbxError(this HttpResponse response, string error)
    {
        var errors = new object[1] { new { code = 1, message = error } };

        await response.WriteAsJsonAsync(new { errors });
    }

    /// <param name="request">The <see cref="HttpRequest" /></param>
    extension(HttpRequest request)
    {
        /// <summary>
        /// Determines if the request has a valid API key in it.
        /// </summary>
        /// <param name="settings">The <see cref="ClientSettingsSettings" /></param>
        /// <returns>[true] if the request has a valid API key, otherwise false.</returns>
        public bool HasValidApiKey(ClientSettingsSettings settings)
        {
            if (settings.ClientSettingsApiKeys.Length == 0) return true;
            if (!request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeaderValues)) return false;

            var apiKeyHeader = apiKeyHeaderValues.First();
            return settings.ClientSettingsApiKeys.Contains(apiKeyHeader);
        }

        /// <summary>
        /// Tries to get an int64 from the request query string.
        /// </summary>
        /// <remarks>This is case-insensitive.</remarks>
        /// <param name="key">The key to look for</param>
        /// <param name="value">The value of the key</param>
        /// <returns>[true] if the key was found and the value is an integer, otherwise false.</returns>
        public bool TryParseInt64FromQuery(string key, out long value)
        {
            value = 0;

            return request.Query.TryGetValue(key, out var valueString) && long.TryParse(valueString, out value);
        }
    }
}
