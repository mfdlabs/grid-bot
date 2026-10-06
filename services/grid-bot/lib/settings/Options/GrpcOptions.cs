namespace Grid.Bot;

using Logging;

/// <summary>
/// Options for the gRPC server.
/// </summary>
public class GrpcOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Grpc";

    /// <summary>
    /// The vault path for the gRPC options.
    /// </summary>
    public const string VaultPath = "grpc";

    /// <summary>
    /// Gets or sets a value indicating whether the grid-bot gRPC server is enabled.
    /// </summary>
    public bool GridBotGrpcServerEnabled { get; set; }

    /// <summary>
    /// Gets or sets the endpoint for the grid-bot gRPC server.
    /// </summary>
    public string GridBotGrpcServerEndpoint { get; set; } = "http://+:5000";

    /// <summary>
    /// Gets or sets the logger name for the gRPC server.
    /// </summary>
    public string GrpcServerLoggerName { get; set; } = "grpc";

    /// <summary>
    /// Gets or sets the logger level for the gRPC server.
    /// </summary>
    public LogLevel GrpcServerLoggerLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Gets or sets a value indicating whether the gRPC server uses TLS.
    /// </summary>
    public bool GrpcServerUseTls { get; set; } = true;

    /// <summary>
    /// Gets or sets the certificate path for the gRPC server. Required when the server is enabled and uses TLS.
    /// </summary>
    public string GrpcServerCertificatePath { get; set; }

    /// <summary>
    /// Gets or sets the certificate password for the gRPC server. Required when the server is enabled and uses TLS.
    /// </summary>
    public string GrpcServerCertificatePassword { get; set; }
}
