namespace Grid;

using System;
using System.Threading;
using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the operation for creating images.
/// </summary>
internal class CreateImageOperation : DockerOperationBase<(string, string), bool>
{
    private readonly IGridServerDockerSettings _GridServerSettings;
    private readonly DockerLogger _DockerLogger;

    /// <summary>
    /// Construct a new instance of <see cref="CreateImageOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <param name="dockerLogger">The <see cref="DockerLogger"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="gridServerSettings"/> cannot be null.
    /// - <paramref name="dockerLogger"/> cannot be null.
    /// </exception>
    public CreateImageOperation(ILogger logger, IDockerClient dockerClient, IGridServerDockerSettings gridServerSettings, DockerLogger dockerLogger)
        : base(logger, dockerClient, "CreateImage")
    {
        _GridServerSettings = gridServerSettings ?? throw new ArgumentNullException(nameof(gridServerSettings));
        _DockerLogger = dockerLogger ?? throw new ArgumentNullException(nameof(dockerLogger));
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync((string, string) input)
    {
        var config = GetAuthConfig();
        Logger.Information($"CreateImageAsync. Got Auth Config. Username = {config?.Username}, IdentityToken = {config?.IdentityToken}");

        using (var cts = new CancellationTokenSource(_GridServerSettings.MaxTimeToWaitForImage))
        {
            var (gridServerImageName, version) = input;
            var parameters = new ImagesCreateParameters()
            {
                FromImage = gridServerImageName ?? "",
                Tag = version ?? ""
            };

            Logger.Information($"CreateImageAsync. Requesting CreateImageAsync Grid Server Container Name = {gridServerImageName}, Version = {version}");

            await DockerClient.Images.CreateImageAsync(parameters, config, _DockerLogger, cts.Token).ConfigureAwait(false);
        }

        return (true, true);
    }

    private AuthConfig GetAuthConfig()
    {
        if (!string.IsNullOrWhiteSpace(_GridServerSettings.DockerRegistryUsername) && !string.IsNullOrWhiteSpace(_GridServerSettings.DockerRegistryPassword))
            return new AuthConfig
            {
                Username = _GridServerSettings.DockerRegistryUsername,
                Password = _GridServerSettings.DockerRegistryPassword
            };

        if (!string.IsNullOrWhiteSpace(_GridServerSettings.DockerRegistryIdentityToken))
            return new AuthConfig
            {
                IdentityToken = _GridServerSettings.DockerRegistryIdentityToken
            };

        return null;
    }
}
