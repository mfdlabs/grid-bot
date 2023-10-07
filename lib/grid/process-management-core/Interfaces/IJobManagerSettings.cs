namespace Grid;

using System;

/// <summary>
/// The settings for the job manager. Usually returned by coordination.
/// </summary>
public interface IJobManagerSettings
{
    /// <summary>
    /// The maximum amount of times an instance can be reused.
    /// </summary>
    int MaxInstanceReuses { get; }

    /// <summary>
    /// The maximum amount of Grid Server instances the job manager can have.
    /// </summary>
    int? MaxGridServerInstances { get; }

    /// <summary>
    /// Amount of threads to use to populate the ready instance pool.
    /// </summary>
    int PopulateReadyGridServerInstanceThreads { get; }

    /// <summary>
    /// The amount of ready instances that should be reserved.
    /// </summary>
    int ReadyInstancesToKeepInReserve { get; }

    /// <summary>
    /// The maximum amount of start attempts for an Grid Server instance.
    /// </summary>
    int GridServerStartAttempts { get; }

    /// <summary>
    /// The maximum amount of time to wait for the Grid Server SOAP port to become available.
    /// </summary>
    TimeSpan GridServerWaitForTcpSleepInterval { get; }
}
