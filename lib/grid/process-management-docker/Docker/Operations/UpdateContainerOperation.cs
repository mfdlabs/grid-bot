namespace Grid;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using Docker.DotNet;
using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents the update container operation.
/// </summary>
internal class UpdateContainerOperation : DockerOperationBase<GridServerContainerUpdateParameters, ContainerUpdateResponse>
{
    private readonly HttpClient _HttpClient;

    /// <summary>
    /// Construct a new instance of <see cref="UpdateContainerOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    /// <param name="httpClient">The <see cref="HttpClient"/></param>
    public UpdateContainerOperation(ILogger logger, IDockerClient dockerClient, HttpClient httpClient)
        : base(logger, dockerClient, "UpdateContainer")
    {
        _HttpClient = httpClient;
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, ContainerUpdateResponse)> DoExecuteAsync(GridServerContainerUpdateParameters updateParameters)
    {
        try
        {
            return (true, await DockerClient.Containers.UpdateContainer(_HttpClient, updateParameters).ConfigureAwait(false));
        }
        catch (Exception ex)
        {
            Logger.Error("UpdateContainer failed for {0}: {1}", updateParameters, ex);

            return (false, null);
        }
    }
}
