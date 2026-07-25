namespace Redis;

using System;
using System.ComponentModel;

/// <summary>
/// Settings for the Hybrid Redis Client Provider.
/// </summary>
public interface IHybridRedisClientProviderSettings : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the initial discovery wait time.
    /// </summary>
    TimeSpan InitialDiscoveryWaitTime { get; }
}
