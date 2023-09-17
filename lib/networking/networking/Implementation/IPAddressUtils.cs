namespace Networking;

using System;
using System.Net;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Simple Utils for IPAddresses
/// </summary>
public static class IPAddressUtils
{
    /// <summary>
    /// Resolve the Client Ip address from the header.
    /// </summary>
    /// <param name="headerValue">Value of the header</param>
    /// <returns>The client IP address</returns>
    /// <exception cref="ArgumentException">Unable to parse ip address</exception>
    public static IPAddress ResolveClientIpAddressFromRequestHeader(string headerValue)
    {
        if (string.IsNullOrEmpty(headerValue))
            return null;

        var ipaddress = ParseIPAddress(headerValue.Split(',')[0]);
        if (ipaddress != null)
            return ipaddress;

        throw new ArgumentException($"Unable to parse ip address '{headerValue}'", nameof(headerValue));
    }

    /// <summary>
    /// Is the specified IP address allowed in the list of ranges.
    /// </summary>
    /// <param name="ipAddress">The IP addresses</param>
    /// <param name="ipAddressRanges">The ranges</param>
    /// <returns>True if allowed, false otherwise.</returns>
    public static bool IsIpAddressAllowed(IPAddress ipAddress, IReadOnlyCollection<IpAddressRange> ipAddressRanges) 
        => ipAddressRanges.Aggregate(false, (result, ipRange) => result || ipRange.IsInRange(ipAddress));

    /// <summary>
    /// Safely parse the specified IP address string.
    /// </summary>
    /// <param name="ipAddressString">The IP address string.</param>
    /// <returns>The parsed address.</returns>
    public static IPAddress ParseIPAddress(string ipAddressString)
    {
        if (!IPAddress.TryParse(ipAddressString, out var result))
            return null;

        return result;
    }
}
