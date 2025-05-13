namespace Grid.Bot;

using System;

using Logging;

/// <summary>
/// Settings provider for all gRPC Server related stuff.
/// </summary>
public class GrpcSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.GrpcPath;

    /// <summary>
    /// Determines if the grid-bot grpc server should be enabled or not.
    /// </summary>
    public bool GridBotGrpcServerEnabled => GetOrDefault(
        nameof(GridBotGrpcServerEnabled),
        false  
    );

    /// <summary>
    /// Gets the endpoint for the Grid Bot gRPC server.
    /// </summary>
    public string GridBotGrpcServerEndpoint => GetOrDefault(
        nameof(GridBotGrpcServerEndpoint),
        "http://+:5000"
    );

    /// <summary>
    /// ASP.NET Core logger name for the gRPC server.
    /// </summary>
    public string GrpcServerLoggerName => GetOrDefault(
        nameof(GrpcServerLoggerName),
        "grpc"
    );

    /// <summary>
    /// ASP.NET Core logger level for the gRPC server.
    /// </summary>
    public LogLevel GrpcServerLoggerLevel => GetOrDefault(
        nameof(GrpcServerLoggerLevel),
        LogLevel.Information
    );

    /// <summary>
    /// Determines if the gRPC server should use TLS.
    /// </summary>
    public bool GrpcServerUseTls => GetOrDefault(
        nameof(GrpcServerUseTls),
        true
    );

    /// <summary>
    /// Gets the certificate path for the gRPC server.
    /// </summary>
    public string GrpcServerCertificatePath => GetOrDefault<string>(
        nameof(GrpcServerCertificatePath),
        () => throw new InvalidOperationException($"'{nameof(GrpcServerCertificatePath)}' is required when '{nameof(GrpcServerUseTls)}' is true.")
    );

    /// <summary>
    /// Gets the certificate password for the gRPC server.
    /// </summary>
    public string GrpcServerCertificatePassword => GetOrDefault<string>(
        nameof(GrpcServerCertificatePassword),
        () => throw new InvalidOperationException($"'{nameof(GrpcServerCertificatePassword)}' is required when '{nameof(GrpcServerUseTls)}' is true.")
    );

}
