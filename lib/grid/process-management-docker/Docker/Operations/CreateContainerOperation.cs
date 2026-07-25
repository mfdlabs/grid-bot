namespace Grid;

using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the operation to create containers.
/// </summary>
internal class CreateContainerOperation : DockerOperationBase<CreateContainerParameters, string>
{
    /// <summary>
    /// Construct a new instance of <see cref="CreateContainerOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    public CreateContainerOperation(ILogger logger, IDockerClient dockerClient)
        : base(logger, dockerClient, "CreateContainer")
    {
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, string)> DoExecuteAsync(CreateContainerParameters parameters)
    {
        var container = await DockerClient.Containers.CreateContainerAsync(parameters).ConfigureAwait(false);

        return string.IsNullOrEmpty(container.ID)
            ? (false, null)
            : (true, container.ID);
    }
}
