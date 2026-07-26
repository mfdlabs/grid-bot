using Docker.DotNet;
using Docker.DotNet.Models;

namespace Grid.ProcessManagement.Docker;

using System;
using System.Threading.Tasks;

using Logging;

/// <summary>
/// Represents the kill container operation.
/// </summary>
internal class StopContainerOperation : DockerOperationBase<string, bool>
{
    private readonly IGridServerDockerSettings _GridServerSettings;

    /// <summary>
    /// Construct a new instance of <see cref="StopContainerOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="gridServerSettings"/> cannot be null.</exception>
    public StopContainerOperation(ILogger logger, IDockerClient dockerClient, IGridServerDockerSettings gridServerSettings)
        : base(logger, dockerClient, "StopContainer")
    {
        _GridServerSettings = gridServerSettings ?? throw new ArgumentNullException(nameof(gridServerSettings));
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync(string containerId)
    {
        var parameters = new ContainerStopParameters
        {
            WaitBeforeKillSeconds = (uint)_GridServerSettings.ContainerStopWaitBeforeKillInSeconds
        };

        await DockerClient.Containers.StopContainerAsync(containerId, parameters).ConfigureAwait(false);

        return (true, true);
    }
}
