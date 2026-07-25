namespace Grid;

using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;


/// <summary>
/// Represents the start container operation.
/// </summary>
internal class StartContainerOperation : DockerOperationBase<string, bool>
{
    private static readonly ContainerStartParameters _StartParameters = new();

    /// <summary>
    /// Construct a new instance of <see cref="StartContainerOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    public StartContainerOperation(ILogger logger, IDockerClient dockerClient)
        : base(logger, dockerClient, "StartContainer")
    {
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync(string containerName)
        => !await DockerClient.Containers.StartContainerAsync(containerName, _StartParameters).ConfigureAwait(false)
            ? (false, false)
            : (true, true);
}
