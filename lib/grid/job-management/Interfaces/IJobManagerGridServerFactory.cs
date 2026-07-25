namespace Grid.JobManagement;

using System;
using System.Runtime.InteropServices;

using Logging;
using ClientSettings.Client;

using ProcessManagement;
using ProcessManagement.Core;
using ProcessManagement.Docker;

/// <summary>
/// Factory for spitting out <see cref="IJobManagerGridServer"/>
/// based on operating system.
/// </summary>
public interface IJobManagerGridServerFactory
{
    /// <summary>
    /// Gets an instance of <see cref="IJobManagerGridServer"/> based on OS.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <param name="clientSettingsClient">The <see cref="IClientSettingsClient"/></param>
    /// <param name="settings">The <see cref="IJobManagerSettings"/> that are either <see cref="IGridServerProcessSettings"/> or <see cref="IGridServerDockerSettings"/></param>
    /// <returns>An instance of <see cref="IJobManagerGridServer"/> proxying either <see cref="ProcessJobManager"/> or <see cref="DockerJobManager"/></returns>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="logger"/> cannot be null.
    /// - <paramref name="clientSettingsClient"/> cannot be null.
    /// - <paramref name="settings"/> cannot be null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// - <paramref name="settings"/> must be of type <see cref="IGridServerProcessSettings"/> when on <see cref="OSPlatform.Windows"/>.
    /// - <paramref name="settings"/> must be of type <see cref="IGridServerDockerSettings"/> when on <see cref="OSPlatform.Linux"/>.
    /// </exception>
    IJobManagerGridServer GetJobManager(ILogger logger, IClientSettingsClient clientSettingsClient, IJobManagerSettings settings);
}
