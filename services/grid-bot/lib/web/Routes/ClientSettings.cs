namespace Grid.Bot.Web.Routes;

using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using Logging;

using Utility;
using Extensions;
using Bot.ClientSettings;

/// <summary>
/// Routes for the client settings API.
/// </summary>
public class ClientSettings
{
    private const string _applicationNameQueryParameter = "applicationName";
    private const string _invalidAppNameError = "The application name is invalid."; // also used for denied access.

    private readonly IOptionsMonitor<ClientSettingsOptions> _options;
    private ClientSettingsOptions _settings => _options.CurrentValue;
    private readonly ILogger _logger;
    private readonly IClientSettingsFactory _clientSettingsFactory;

    /// <summary>
    /// Construct a new instance of <see cref="ClientSettings"/>
    /// </summary>
    /// <param name="options">The <see cref="ClientSettingsOptions"/></param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="clientSettingsFactory">The <see cref="IClientSettingsFactory"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="options"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="clientSettingsFactory"/> cannot be null.
    /// </exception>
    public ClientSettings(IOptionsMonitor<ClientSettingsOptions> options, ILogger logger, IClientSettingsFactory clientSettingsFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientSettingsFactory = clientSettingsFactory ?? throw new ArgumentNullException(nameof(clientSettingsFactory));
    }

    /// <summary>
    /// Get application settings for the specified application.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext" /></param>
    public async Task GetApplicationSettings(HttpContext context)
    {
        if (!context.Request.Query.TryGetValue(_applicationNameQueryParameter, out var applicationNameValues))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteRbxError(_invalidAppNameError);

            return;
        }

        var applicationName = applicationNameValues.First();

        if (!_settings.PermissibleReadApplications.Contains(applicationName) && !context.Request.HasValidApiKey(_settings))
        {
            _logger.Warning("User {0} read attempt on non permissible read application ({1})", context.Connection.RemoteIpAddress, applicationName);

            context.Response.StatusCode = 403;
            await context.Response.WriteRbxError(_invalidAppNameError);

            return;
        }

        var applicationSettings = _clientSettingsFactory.GetSettingsForApplication(applicationName);
        if (applicationSettings is null)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteRbxError(_invalidAppNameError);

            return;
        }

        await context.Response.WriteAsJsonAsync(new { applicationSettings });
    }
}
