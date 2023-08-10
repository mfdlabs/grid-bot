namespace ServiceDiscovery;

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

    internal LocalIpAddressProvider(IDns dns, ILogger logger)
    {
        _Dns = dns ?? throw new ArgumentNullException(nameof(dns));
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        NetworkChange.NetworkAddressChanged += (sender, args) => RefreshIpAddresses();

        RefreshIpAddresses();

        _Logger.Information("{0} initialized with IPv4 address: {1}", nameof(LocalIpAddressProvider), AddressV4);
        _Logger.Information("{0} initialized with IPv6 address: {1}", nameof(LocalIpAddressProvider), AddressV6);
    }

    /// <inheritdoc cref="ILocalIpAddressProvider.GetIpAddressesV4"/>
    public IList<IPAddress> GetIpAddressesV4()
        => GetIpAddresses(AddressFamily.InterNetwork).ToList();

    /// <inheritdoc cref="ILocalIpAddressProvider.GetIpAddressesV6"/>
    public IList<IPAddress> GetIpAddressesV6() 
        => GetIpAddresses(AddressFamily.InterNetworkV6).ToList();

    internal void RefreshIpAddresses()
    {
        RefreshIpAddresses(AddressFamily.InterNetwork);
        RefreshIpAddresses(AddressFamily.InterNetworkV6);
    }

    internal virtual void RefreshIpAddresses(AddressFamily addressFamily)
    {
        if (addressFamily != AddressFamily.InterNetwork && addressFamily != AddressFamily.InterNetworkV6)
        {
            _Logger.Error("Unsupported address family: {0}", addressFamily);
            return;
        }

        var addresses = GetIpAddresses(addressFamily).ToList();
        if (!addresses.Any())
        {
            _Logger.Error("No public {0} address found for server, while trying to refresh IP address in {1}!", addressFamily, nameof(RefreshIpAddresses));
            return;
        }

        if ((addressFamily == AddressFamily.InterNetwork && addresses.Contains(AddressV4)) ||
            (addressFamily == AddressFamily.InterNetworkV6 && addresses.Contains(AddressV6)))
            return;

        var newIpAddress = addresses.First();

        IPAddress oldIpAddress;
        if (addressFamily == AddressFamily.InterNetwork)
        {
            oldIpAddress = AddressV4;

            AddressV4 = newIpAddress;
        }
        else
        {
            oldIpAddress = AddressV6;

            AddressV6 = newIpAddress;
        }

        _Logger.Information("{0} IP address changed from {1} to {2}.", addressFamily, oldIpAddress, newIpAddress);
    }

    internal virtual IEnumerable<IPAddress> GetIpAddresses(AddressFamily addressFamily)
    {
        IPHostEntry hostEntry;
        try
        {
            hostEntry = _Dns.GetHostEntry(_Dns.GetHostName());
        }
        catch (Exception ex)
        {
            _Logger.Error("Exception encountered while acquiring host information from DNS: {0}", ex);

            return Enumerable.Empty<IPAddress>();
        }
        if (hostEntry == null)
            return Enumerable.Empty<IPAddress>();

        return (from address in hostEntry.AddressList
                where address.AddressFamily == addressFamily
                select address).ToList();
    }
}
