namespace Networking;

using System;
using System.Collections.Generic;

/// <summary>
/// Simple class for parsing IP prefixes
/// </summary>
public class IpPrefixParser
{
    private const int _Mask = 1;

    /// <summary>
    /// Compute the IP addresses within the following IP prefix.
    /// 
    /// 10.0.0.0/30 -&gt; List&lt;string&gt;(4) { "10.0.0.0", "10.0.0.1", "10.0.0.2", "10.0.0.3" }
    /// </summary>
    /// <param name="ipPrefix">The prefix string</param>
    /// <returns>The IP addresses within the prefix.</returns>
    public static List<string> ComputeIpAddressesFromPrefix(string ipPrefix)
    {
        var split = ipPrefix.Split('/');
        var ip = split[0];
        var mask = int.Parse(split[1]);

        var addressParts = ip.Split('.');

        int address = int.Parse(addressParts[0]) << 24 
            | int.Parse(addressParts[1]) << 16 
            | int.Parse(addressParts[2]) << 8 
            | int.Parse(addressParts[3]);

        for (int i = mask; i < 32; i++)
            address &= ~(_Mask << 31 - i);

        var maskAddress = (int)Math.Pow(2, 32 - mask);
        var addresses = new List<string>(maskAddress);
        for (int j = 0; j < maskAddress; j++)
        {
            var newAddress = address | j;
            addresses.Add(
                string.Join(
                    ".", 
                    (newAddress & 0xff000000) >> 24 & 0xff,
                    (newAddress & 0xff0000) >> 16,
                    (newAddress & 0xff00) >> 8,
                    newAddress & 0xff
            ));
        }

        return addresses;
    }
}
