namespace Grid;

using System;
using System.Threading;
using System.Threading.Tasks;

using Docker.DotNet;

using Logging;

/// <summary>
/// Represents the operation to check images.
/// </summary>
internal class CheckImageOperation : DockerOperationBase<(string, string), bool>
{
    private readonly IGridServerDockerSettings _GridServerSettings;

    /// <summary>
    /// Construct a new instance of <see cref="CheckImageOperation"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="dockerClient">The <see cref="IDockerClient"/></param>
    /// <param name="gridServerSettings">The <see cref="IGridServerDockerSettings"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="gridServerSettings"/> cannot be null.</exception>
    public CheckImageOperation(ILogger logger, IDockerClient dockerClient, IGridServerDockerSettings gridServerSettings)
        : base(logger, dockerClient, "CheckImage")
    {
        _GridServerSettings = gridServerSettings ?? throw new ArgumentNullException(nameof(gridServerSettings));
    }

    /// <inheritdoc cref="DockerOperationBase{TInput, TOutput}.DoExecuteAsync(TInput)"/>
    protected async override Task<(bool, bool)> DoExecuteAsync((string, string) input)
    {
        var (ContainerName, Version) = input;

        using (var cts = new CancellationTokenSource(_GridServerSettings.MaxTimeToWaitForInspectImage))
            if (await DockerClient.Images.InspectImageAsync($"{ContainerName}:{Version}", cts.Token).ConfigureAwait(false) != null)
                return (true, true);

        return (false, false);
    }

}
