namespace Grid;

using System;
using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the kill container operation.
/// </summary>
internal class KillContainerOperation : DockerOperationBase<string, bool>
{
    private static readonly ContainerKillParameters _KillParameters = new();

    /// <summary>
    /// Construct a new instance of <see cref="KillContainerOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    public KillContainerOperation(ILogger logger, IDockerClient dockerClient)
        : base(logger, dockerClient, "KillContainer")
    {
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync(string containerId)
    {
        await DockerClient.Containers.KillContainerAsync(containerId, _KillParameters).ConfigureAwait(false);

        return (true, true);
    }
}
