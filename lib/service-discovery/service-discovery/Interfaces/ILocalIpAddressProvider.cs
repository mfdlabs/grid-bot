namespace ServiceDiscovery;

using System.Net;
using System.ComponentModel;
using System.Collections.Generic;

/// <summary>
/// Local Ip provider.
/// </summary>
public interface ILocalIpAddressProvider : INotifyPropertyChanged
{
    /// <summary>
    /// The IPv4 address
    /// </summary>
    IPAddress AddressV4 { get; }

    /// <summary>
    /// The IPv6 address
    /// </summary>
    IPAddress AddressV6 { get; }

    /// <summary>
    /// Get all IPv4 addresses
    /// </summary>
    /// <returns>All IPv4 addresses</returns>
    IList<IPAddress> GetIpAddressesV4();

    /// <summary>
    /// Get all IPv6 addresses
    /// </summary>
    /// <returns>All IPv6 addresses</returns>
    IList<IPAddress> GetIpAddressesV6();
}
