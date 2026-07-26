namespace Grid.ProcessManagement.Core;

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

    /// <summary>
    /// The Grid Server application settings name.
    /// </summary>
    string GridServerSettingsApplicationName { get; }

    /// <summary>
    /// The Grid Server applicatiom bucket name.
    /// </summary>
    string GridServerSettingsBucketName { get; }

    /// <summary>
    /// The Grid Server application settings file path.
    /// </summary>
    string GridServerApplicationSettingsFilePath { get; }

    /// <summary>
    /// The valid window in which to update application settings.
    /// </summary>
    TimeSpan GridServerApplicationSettingsValidWindow { get; }

    /// <summary>
    /// Grid Server max threads.
    /// </summary>
    public int GridServerMaxThreads { get; }

    /// <summary>
    /// Is Grid Server CPU allocation check enabled.
    /// </summary>
    public bool IsGridServerCpuAllocationCheckEnabled { get; }

    /// <summary>
    /// Is Grid Server threads allocation check enabled?
    /// </summary>
    public bool IsGridServerThreadsAllocationCheckEnabled { get; }

    /// <summary>
    /// Is Grid Server memory allocation check enabled?
    /// </summary>
    public bool IsGridServerMemoryAllocationCheckEnabled { get; }

    /// <summary>
    /// Grid Server CPU over-allocation ratio.
    /// </summary>
    public double GridServerCpuOverAllocationRatio { get; }

    /// <summary>
    /// Grid Server threads over-allocation ratio.
    /// </summary>
    public double GridServerThreadsOverAllocationRatio { get; }

    /// <summary>
    /// Grid Server memory over-allocation ratio.
    /// </summary>
    public double GridServerMemoryOverAllocationRatio { get; }
}
