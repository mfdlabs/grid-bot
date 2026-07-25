namespace Grid;

using System.Threading;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the <see cref="IUnmanagedGridServerInstance"/> implementation for Grid Server Docker containers.
/// </summary>
public sealed class UnmanagedGridServerDockerContainer : IUnmanagedGridServerInstance
{
    private const int _MillisecondsInSecond = 1000;

    private readonly ILogger _Logger;
    private readonly DockerClient _DockerClient;
    private readonly IGridServerDockerSettings _GridServerSettings;

    /// <inheritdoc cref="IUnmanagedGridServerInstance.Id"/>
    public string Id => Container.ID;

    /// <summary>
    /// The container.
    /// </summary>
    public ContainerListResponse Container { get; }

    /// <summary>
    /// Contruct a new instance of <see cref="UnmanagedGridServerDockerContainer"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="DockerClient"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <param name="container">The <see cref="ContainerListResponse"/></param>
    public UnmanagedGridServerDockerContainer(ILogger logger, DockerClient dockerClient, IGridServerDockerSettings gridServerSettings, ContainerListResponse container)
    {
        _Logger = logger;
        _DockerClient = dockerClient;
        _GridServerSettings = gridServerSettings;

        Container = container;
    }

    /// <inheritdoc cref="IUnmanagedGridServerInstance.Kill"/>
    public void Kill()
    {
        int maxAttemptsToWaitForContainerExit = _GridServerSettings.MaxAttemptsToWaitForContainerExit;

        int attempts = 0;
        while (attempts < maxAttemptsToWaitForContainerExit &&
              !_DockerClient.Containers.StopContainerAsync(
                   Container.ID,
                   new ContainerStopParameters
                   {
                       WaitBeforeKillSeconds = (uint)_GridServerSettings.ContainerStopWaitBeforeKillInSeconds
                   }
               ).Result
        )
        {
            _Logger.Debug("GridServerDockerContainer.Dispose(): waiting for container to exit. Attempt # {0}", attempts + 1);

            Thread.Sleep(_MillisecondsInSecond);

            attempts++;
        }

        _DockerClient.Containers.RemoveContainerAsync(
            Container.ID,
            new ContainerRemoveParameters
            {
                RemoveVolumes = true,
                Force = true
            }
        ).Wait();
    }
}
