namespace Grid;

using System.Threading.Tasks;
using System.Collections.Generic;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the has-exited operation.
/// </summary>
internal class HasExitedOperation : DockerOperationBase<string, bool>
{
    /// <summary>
    /// Construct a new instance of <see cref="HasExitedOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    public HasExitedOperation(ILogger logger, IDockerClient dockerClient)
        : base(logger, dockerClient, "HasExited", LogLevel.Debug)
    {
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync(string containerName)
    {
        var parameters = new ContainersListParameters
        {
            Filters = new Dictionary<string, IDictionary<string, bool>>
            {
                { "name", new Dictionary<string, bool>() { [$"/{containerName}"] = true } },
                { "status", new Dictionary<string, bool>() { ["running"] = true } }
            }
        };

        if ((await DockerClient.Containers.ListContainersAsync(parameters).ConfigureAwait(false)).Count != 0)
            return (true, false);

        Logger.Warning("Unable to find matching container with name {0} assuming the container has exited", containerName);

        return (true, true);
    }
}
