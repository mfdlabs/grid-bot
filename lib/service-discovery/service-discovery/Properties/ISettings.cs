namespace ServiceDiscovery.Properties;

using System;
using System.ComponentModel;

/// <summary>
/// Shared settings for Consul.
/// </summary>
public interface ISettings : INotifyPropertyChanged
{
    /// <summary>
    /// The address for Consul.
    /// </summary>
    string ConsulAddress { get; }

    /// <summary>
    /// Backoff base for consul when retrying.
    /// </summary>
    TimeSpan ConsulBackoffBase { get; }

    /// <summary>
    /// Long polling wait time.
    /// </summary>
    TimeSpan ConsulLongPollingMaxWaitTime { get; }

    /// <summary>
    /// Max backoff when retrying.
    /// </summary>
    TimeSpan MaximumConsulBackoff { get; }

    /// <summary>
    /// Gets the refresh interval.
    /// </summary>
    TimeSpan ConsulRefreshInterval { get; }
}
