namespace Grid.Bot;

using Logging;

/// <summary>
/// Options for the web server.
/// </summary>
public class WebOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Web";

    /// <summary>
    /// The vault path for the web options.
    /// </summary>
    public const string VaultPath = "web";

    /// <summary>
    /// The proxy networks allowed when none are configured.
    /// </summary>
    public static readonly string[] DefaultAllowedProxyRanges = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];

    /// <summary>
    /// Gets or sets a value indicating whether the web server is enabled.
    /// </summary>
    public bool IsWebServerEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the bind address for the web server.
    /// </summary>
    public string WebServerBindAddress { get; set; } = "http://+:8888";

    /// <summary>
    /// Gets or sets a value indicating whether the web server is behind a reverse proxy.
    /// </summary>
    /// <remarks>
    /// If true, then x-forwarded-for and x-forwarded-proto headers will be used to determine the client IP address.
    /// </remarks>
    public bool IsWebServerBehindProxy { get; set; }

    /// <summary>
    /// Gets or sets the allowed proxy networks, defaulting to <see cref="DefaultAllowedProxyRanges"/> when empty.
    /// </summary>
    public string[] WebServerAllowedProxyRanges { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the web server uses TLS.
    /// </summary>
    /// <remarks>
    /// This should be set to true always in post-2017 grid servers
    /// if this is the standard web server.
    /// </remarks>
    public bool WebServerUseTls { get; set; }

    /// <summary>
    /// Gets or sets the path to the PFX certificate. Required when the server is enabled and uses TLS.
    /// </summary>
    public string WebServerCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the password for the PFX certificate. Required when the server is enabled and uses TLS.
    /// </summary>
    public string WebServerCertificatePassword { get; set; }

    /// <summary>
    /// Gets or sets the ASP.NET Core logger name for the web server.
    /// </summary>
    public string WebServerLoggerName { get; set; } = "web";

    /// <summary>
    /// Gets or sets the ASP.NET Core logger level for the web server.
    /// </summary>
    public LogLevel WebServerLoggerLevel { get; set; } = LogLevel.Information;
}
