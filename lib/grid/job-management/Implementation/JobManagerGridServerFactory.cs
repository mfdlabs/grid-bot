namespace Grid.JobManagement;

using System;
using System.Runtime.InteropServices;

using Random;
using Logging;
using ClientSettings.Client;

using PortManagement;
using ProcessManagement;
using ProcessManagement.Core;
using ProcessManagement.Docker;

/// <summary>
/// Default implementation of <see cref="IJobManagerGridServerFactory"/>
/// </summary>
public class JobManagerGridServerFactory : IJobManagerGridServerFactory
{
    /// <inheritdoc cref="IJobManagerGridServerFactory.GetJobManager(ILogger, IClientSettingsClient, IJobManagerSettings)"/>
    public IJobManagerGridServer GetJobManager(ILogger logger, IClientSettingsClient clientSettingsClient, IJobManagerSettings settings)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        if (clientSettingsClient == null) throw new ArgumentNullException(nameof(clientSettingsClient));
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        var portAllocator = new PortAllocator(logger);

        JobManagerBase jobManager;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (settings is not IGridServerProcessSettings processSettings)
                throw new ArgumentException($"{nameof(settings)} must be of type {nameof(IGridServerProcessSettings)} when on {OSPlatform.Windows}.", nameof(settings));

            jobManager = new ProcessJobManager(
                logger,
                portAllocator,
                processSettings,
                clientSettingsClient
            );
        }
        else
        {
            if (settings is not IGridServerDockerSettings dockerSettings)
                throw new ArgumentException($"{nameof(settings)} must be of type {nameof(IGridServerProcessSettings)} when on {RuntimeInformation.OSDescription}.", nameof(settings));

            jobManager = new DockerJobManager(
                logger,
                portAllocator,
                dockerSettings,
                RandomFactory.GetDefaultRandom(),
                clientSettingsClient
            );
        }

        return new JobManagerGridServer(logger, jobManager);
    }
}
