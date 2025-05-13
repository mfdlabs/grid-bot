namespace Grid.Bot;

using System;
using System.Collections.Generic;

using Logging;

/// <summary>
/// Settings provider for all Web Server related stuff.
/// </summary>
public class WebSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.WebPath;

    /// <summary>
    /// Determines if the web server should be enabled.
    /// </summary>
    public bool IsWebServerEnabled => GetOrDefault(nameof(IsWebServerEnabled), true);

    /// <summary>
    /// Gets the bind address for the web server.
    /// </summary>
    public string WebServerBindAddress => GetOrDefault(nameof(WebServerBindAddress), "http://+:8080");

    /// <summary>
    /// Determines if the web server is behind a reverse proxy.
    /// </summary>
    /// <renarks>
    /// If true, then x-forwarded-for and x-forwarded-proto headers will be used to determine the client IP address.
    /// </renarks>
    public bool IsWebServerBehindProxy => GetOrDefault(nameof(IsWebServerBehindProxy), false);

    /// <summary>
    /// Gets the list of allowed proxy networks.
    /// </summary>
    public string[] WebServerAllowedProxyRanges => GetOrDefault(nameof(WebServerAllowedProxyRanges), new[] { "10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16" });

    /// <summary>
    /// Determines if the web server should use TLS.
    /// </summary>
    /// <remarks>
    /// This should be set to true always in post-2017 grid servers
    /// if this is the standard web server.
    /// </remarks>
    public bool WebServerUseTls => GetOrDefault(nameof(WebServerUseTls), false);

    /// <summary>
    /// Gets the path to the PFX certificate for the web server.
    /// </summary>
    public string WebServerCertificatePath => GetOrDefault<string>(
        nameof(WebServerCertificatePath),
        () => throw new InvalidOperationException($"'{nameof(WebServerCertificatePath)}' is required when '{nameof(WebServerUseTls)}' is true.")
    );

    /// <summary>
    /// Gets the password for the PFX certificate for the web server.
    /// </summary>
    public string WebServerCertificatePassword => GetOrDefault<string>(
        nameof(WebServerCertificatePassword),
        () => throw new InvalidOperationException($"'{nameof(WebServerCertificatePassword)}' is required when '{nameof(WebServerUseTls)}' is true.")
    );

    /// <summary>
    /// Gets the ASP.NET Core logger name for the web server.
    /// </summary>
    public string WebServerLoggerName => GetOrDefault(nameof(WebServerLoggerName), "web");

    /// <summary>
    /// Gets the ASP.NET Core logger level for the web server.
    /// </summary>
    public LogLevel WebServerLoggerLevel => GetOrDefault(nameof(WebServerLoggerLevel), LogLevel.Information);
}
