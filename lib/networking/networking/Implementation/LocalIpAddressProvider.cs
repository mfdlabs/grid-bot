namespace Networking;

using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net.NetworkInformation;

using Logging;

/// <inheritdoc cref="ILocalIpAddressProvider"/>
public class LocalIpAddressProvider : ILocalIpAddressProvider, INotifyPropertyChanged
{
    private static ILocalIpAddressProvider _singleton;
    private static readonly object _lock = new();

    /// <summary>
    /// Singleton instance of <see cref="ILocalIpAddressProvider"/>
    /// </summary>
    public static ILocalIpAddressProvider Singleton
    {
        get
        {
            lock (_lock)
                return _singleton ??= new LocalIpAddressProvider(Logger.Singleton);
        }
    }

    private readonly IDns _Dns;
    private readonly ILogger _Logger;
    private readonly object _AddressV4Lock = new();
    private readonly object _AddressV6Lock = new();

    private IPAddress _AddressV4;
    private IPAddress _AddressV6;

    /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <inheritdoc cref="ILocalIpAddressProvider.AddressV4"/>
    public virtual IPAddress AddressV4
    {
        get => _AddressV4;
        internal set
        {
            if (value.Equals(_AddressV4)) return;

            lock (_AddressV4Lock)
            {
                if (value.Equals(_AddressV4)) return;

                _AddressV4 = value;
            }

            PropertyChanged?.Invoke(this, new(nameof(AddressV4)));
        }
    }

    /// <inheritdoc cref="ILocalIpAddressProvider.AddressV6"/>
    public virtual IPAddress AddressV6
    {
        get => _AddressV6;
        internal set
        {
            if (value.Equals(_AddressV6)) return;

            lock (_AddressV6Lock)
            {
                if (value.Equals(_AddressV6)) return;

                _AddressV6 = value;
            }

            PropertyChanged?.Invoke(this, new(nameof(AddressV6)));
        }
    }

    /// <summary>
    /// Construct a new instance of <see cref="LocalIpAddressProvider"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be null.</exception>
    public LocalIpAddressProvider(ILogger logger)
        : this(new DnsWrapper(), logger)
    {
    }

    /// <summary>
    /// Construct a new instance of <see cref="LocalIpAddressProvider"/>
    /// </summary>
    /// <param name="dns">The <see cref="IDns"/></param>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <exception cref="ArgumentNullException">
    /// - <paramref name="dns"/> cannot be null.
    /// - <paramref name="logger"/> cannot be null.
    /// </exception>
    public LocalIpAddressProvider(IDns dns, ILogger logger)
    {
        _Dns = dns ?? throw new ArgumentNullException(nameof(dns));
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        NetworkChange.NetworkAddressChanged += (sender, args) => RefreshIpAddresses();

        RefreshIpAddresses();

        _Logger.Information("{0} initialized with IPv4 address: {1}", nameof(LocalIpAddressProvider), AddressV4);
        _Logger.Information("{0} initialized with IPv6 address: {1}", nameof(LocalIpAddressProvider), AddressV6);
    }

    /// <inheritdoc cref="ILocalIpAddressProvider.GetHostName"/>
    public string GetHostName() => _Dns.GetHostName();

    /// <inheritdoc cref="ILocalIpAddressProvider.GetIpAddressesV4"/>
    public IList<IPAddress> GetIpAddressesV4()
        => GetIpAddresses(AddressFamily.InterNetwork).ToList();

    /// <inheritdoc cref="ILocalIpAddressProvider.GetIpAddressesV6"/>
    public IList<IPAddress> GetIpAddressesV6()
        => GetIpAddresses(AddressFamily.InterNetworkV6).ToList();

    private IEnumerable<IPAddress> GetIpAddresses(AddressFamily addressFamily)
    {
        if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
        {
            _Logger.Error("Unsupported address family: {0}", addressFamily);

            return Enumerable.Empty<IPAddress>();
        }

        if (GetAddressesByInterface(NetworkInterfaceType.Wireless80211, addressFamily, out var ipAddresses) ||
            GetAddressesByInterface(NetworkInterfaceType.Ethernet, addressFamily, out ipAddresses) ||
            GetAddressesByInterface(NetworkInterfaceType.Loopback, addressFamily, out ipAddresses))
            return ipAddresses;

        _Logger.Error("No public {0} address found for server on the following interfaces: {1}, {2}, {3}!", addressFamily, NetworkInterfaceType.Wireless80211, NetworkInterfaceType.Ethernet, NetworkInterfaceType.Loopback);

        return Enumerable.Empty<IPAddress>();
    }

    private static bool GetAddressesByInterface(NetworkInterfaceType interfaceType, AddressFamily addressFamily, out IEnumerable<IPAddress> ipAddresses)
        => (ipAddresses = (from item in NetworkInterface.GetAllNetworkInterfaces()
                  where item.NetworkInterfaceType == interfaceType &&
                        item.OperationalStatus == OperationalStatus.Up
                  select item.GetIPProperties().UnicastAddresses
                  into unicastAddresses
                  select unicastAddresses.FirstOrDefault()?.Address
                  into address
                  where address != null && 
                        address.AddressFamily == addressFamily
                  select address))?.Any() ?? false;

    private void RefreshIpAddresses(AddressFamily addressFamily)
    {
        if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
        {
            _Logger.Error("Unsupported address family: {0}", addressFamily);

            return;
        }

        // Try Wi-Fi first, then Ethernet and finally Loopback
        if (GetAddressesByInterface(NetworkInterfaceType.Wireless80211, addressFamily, out var newIpAddresses) ||
            GetAddressesByInterface(NetworkInterfaceType.Ethernet, addressFamily, out newIpAddresses) ||
            GetAddressesByInterface(NetworkInterfaceType.Loopback, addressFamily, out newIpAddresses))
        {
            var newIpAddress = newIpAddresses.First();
            var oldIpAddress = addressFamily == AddressFamily.InterNetwork ? AddressV4 : AddressV6;

            if (newIpAddress.Equals(oldIpAddress)) return;

            if (addressFamily == AddressFamily.InterNetwork)
                AddressV4 = newIpAddress;
            else
                AddressV6 = newIpAddress;

            _Logger.Information("{0} IP address changed from {1} to {2}.", addressFamily, oldIpAddress, newIpAddress);
        }
        else
            _Logger.Error("No public {0} address found for server on the following interfaces: {1}, {2}, {3}!", addressFamily, NetworkInterfaceType.Wireless80211, NetworkInterfaceType.Ethernet, NetworkInterfaceType.Loopback);
    }

    internal void RefreshIpAddresses()
    {
        RefreshIpAddresses(AddressFamily.InterNetwork);
        RefreshIpAddresses(AddressFamily.InterNetworkV6);
    }
}
