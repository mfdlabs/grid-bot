namespace Grid;

using System;

using Docker.DotNet.Models;

using Logging;

/// <summary>
/// Represents an <see cref="ILogger"/> wrapper.
/// </summary>
public class DockerLogger : IProgress<JSONMessage>
{
    private readonly ILogger _Logger;

    /// <summary>
    /// Construct a new instance of <see cref="DockerLogger"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    public DockerLogger(ILogger logger)
    {
        _Logger = logger;
    }

    /// <inheritdoc cref="IProgress{T}.Report(T)"/>
    public void Report(JSONMessage value)
    {
        _Logger.Information("Docker progress log: {0} {1}", value.Status, value.ProgressMessage);
    }
}
