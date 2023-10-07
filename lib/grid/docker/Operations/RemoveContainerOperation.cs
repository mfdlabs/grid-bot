namespace Grid;

using System;
using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the remove container operation.
/// </summary>
internal class RemoveContainerOperation : DockerOperationBase<string, bool>
{
    private readonly IGridServerDockerSettings _GridServerSettings;

    /// <summary>
    /// Construct a new instance of <see cref="RemoveContainerOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="gridServerSettings"/> cannot be null.</exception>
    public RemoveContainerOperation(ILogger logger,  IDockerClient dockerClient, IGridServerDockerSettings gridServerSettings)
        : base(logger, dockerClient, "RemoveContainer")
    {
        _GridServerSettings = gridServerSettings ?? throw new ArgumentNullException(nameof(gridServerSettings));
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync(string containerId)
    {
        var parameters = new ContainerRemoveParameters
        {
            RemoveVolumes = _GridServerSettings.IsRemoveVolumesEnabled,
            Force = true
        };

        await DockerClient.Containers.RemoveContainerAsync(containerId, parameters).ConfigureAwait(false);

        return (true, true);
    }
}
