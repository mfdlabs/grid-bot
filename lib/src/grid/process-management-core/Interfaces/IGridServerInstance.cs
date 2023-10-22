namespace Grid;

using System;
using System.Diagnostics;

using ComputeCloud;

/// <summary>
/// Represents a Grid Server Instance.
/// </summary>
public interface IGridServerInstance : IDisposable
{
    /// <summary>
    /// The ID of the instance.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// The name of the instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Has the instanc exited yet?
    /// </summary>
    bool HasExited { get; }

    /// <summary>
    /// The instance expiration time.
    /// </summary>
    DateTime ExpirationTime { get; set; }

    /// <summary>
    /// The maximum use count of the instance.
    /// </summary>
    int UseCount { get; set; }

    /// <summary>
    /// The maximum amount of cores this instance can take.
    /// </summary>
    double MaximumCores { get; set; }

    /// <summary>
    /// The maximum amount of threads this instance can take.
    /// </summary>
    long MaximumThreads { get; set; }

    /// <summary>
    /// The maximum amount of memory this instance can take in MiB.
    /// </summary>
    long MaximumMemoryInMegabytes { get; set; }

    /// <summary>
    /// The port of this instance.
    /// </summary>
    int Port { get; }

    /// <summary>
    /// The RCC version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Get the SOAP interface for this Grid Server Instance.
    /// </summary>
    /// <param name="timeoutInMilliseconds">The timeout of each SOAP action in milliseconds.</param>
    /// <returns>The SOAP interface.</returns>
    ComputeCloudServiceSoap GetSoapInterface(int timeoutInMilliseconds);

    /// <summary>
    /// Start the RCC instance.
    /// </summary>
    /// <returns>True if the instance was started.</returns>
    bool Start();

    /// <summary>
    /// Wait for the instance to become available.
    /// </summary>
    /// <param name="forceTry">Should force try to open.</param>
    /// <param name="stopwatch">The stop watch to be used to determine the total opening time.</param>
    /// <exception cref="Exception">Failed to connect to port in max attempts.</exception>
    void WaitForServiceToBecomeAvailable(bool forceTry, Stopwatch stopwatch);

    /// <summary>
    /// Update the resource limits for this RCC instance.
    /// </summary>
    /// <param name="maximumCores">The new max cores.</param>
    /// <param name="maximumThreads">The new max threads.</param>
    /// <param name="maximumMemoryInMegabytes">The new max memory.</param>
    void UpdateResourceLimits(double maximumCores, long maximumThreads, long maximumMemoryInMegabytes);
}
