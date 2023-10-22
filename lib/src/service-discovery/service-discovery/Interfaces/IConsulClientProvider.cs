namespace ServiceDiscovery;

using System.ComponentModel;

using Consul;

/// <summary>
/// Provider for generating consul clients.
/// </summary>
public interface IConsulClientProvider : INotifyPropertyChanged
{
    /// <summary>
    /// The Consul client.
    /// </summary>
    IConsulClient Client { get; }
}
