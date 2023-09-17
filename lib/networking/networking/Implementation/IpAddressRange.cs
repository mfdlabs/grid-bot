namespace Networking;

using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

/// <summary>
/// Simple class that represents an IP Range
/// </summary>
public class IpAddressRange
{
    private readonly AddressFamily _AddressFamily;
    private readonly byte[] _LowerBytes;
    private readonly byte[] _UpperBytes;

    /// <summary>
    /// Construct a new instance of <see cref="IpAddressRange"/>
    /// </summary>
    /// <param name="lower">The lower IP address of the range.</param>
    /// <param name="upper">The upper IP address of the range.</param>
    public IpAddressRange(IPAddress lower, IPAddress upper)
    {
        _AddressFamily = lower.AddressFamily;
        _LowerBytes = lower.GetAddressBytes();
        _UpperBytes = upper.GetAddressBytes();
    }

    /// <summary>
    /// Does the following IP address fall under this range?
    /// </summary>
    /// <param name="address">The IP address.</param>
    /// <returns>True if the address falls under this range, false otherwise.</returns>
    public bool IsInRange(IPAddress address)
    {
        if (address.AddressFamily != _AddressFamily)
            return false;

        var addressBytes = address.GetAddressBytes();

        bool isGreaterThanLower = true;
        bool isLessThanUpper = true;

        for (int i = 0; i < _LowerBytes.Length && (isGreaterThanLower || isLessThanUpper); i++)
        {
            if ((isGreaterThanLower && addressBytes[i] < _LowerBytes[i]) || (isLessThanUpper && addressBytes[i] > _UpperBytes[i]))
                return false;

            isGreaterThanLower &= (addressBytes[i] == _LowerBytes[i]);
            isLessThanUpper &= (addressBytes[i] == _UpperBytes[i]);
        }

        return true;
    }

    /// <summary>
    /// Construct a collection of IP ranges based on a range string:
    /// 
    /// 127.0.0.0-127.255.255.255,10.0.0.0-10.255.255.255
    /// </summary>
    /// <param name="ranges">The string form of the ranges.</param>
    /// <returns>The constructed ranges.</returns>
    public static IReadOnlyCollection<IpAddressRange> ParseStringList(string ranges)
    {
        if (string.IsNullOrWhiteSpace(ranges))
            return new List<IpAddressRange>().AsReadOnly();

        return (from range in ranges.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                select range.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries).Select(TryGetIpAddress).ToList()
                into range
                where range.First() != null && range.Last() != null
                select range into ips
                select new IpAddressRange(ips.First(), ips.Last())).ToList().AsReadOnly();
    }

    private static IPAddress TryGetIpAddress(string ipString)
    {
        IPAddress.TryParse(ipString.Trim(), out var result);
        return result;
    }
}
