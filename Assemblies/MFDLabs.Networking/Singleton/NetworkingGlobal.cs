using System;
using System.Net;
using System.Net.Sockets;
using MFDLabs.Networking.Extensions;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Networking
{
    public static class NetworkingGlobal
    {
        public static string GenerateUuidv4() => Guid.NewGuid().ToString();
        public static string LocalIp => GetLocalIpAsInt().ToString();
        public static string GetLocalIp()
        {
            if (!global::MFDLabs.Networking.Properties.Settings.Default.LocalIPOverride.IsNullWhiteSpaceOrEmpty())
                return global::MFDLabs.Networking.Properties.Settings.Default.LocalIPOverride;

            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "0.0.0.0";
        }

        public static bool IsIpv4(string ip)
        {
            if (!IPAddress.TryParse(ip, out var address)) return false;

            return address.AddressFamily == AddressFamily.InterNetwork;
        }

        public static bool IsIpv6(string ip)
        {
            if (!IPAddress.TryParse(ip, out var address)) return false;

            return address.AddressFamily == AddressFamily.InterNetworkV6;
        }

        /// <summary>
        /// Determines if the given ip is in the IP range notation like this:
        /// 
        /// var ip = "127.0.0.1";
        /// var range = "127.0.0.-127.255.255.255";
        /// 
        /// var isInRange = NetworkingGlobal.IsIpInRange(ip, range);
        /// </summary>
        /// <param name="ip">The IP Address</param>
        /// <param name="range">The IP Range</param>
        /// <returns>true if the IP is in the range</returns>
        public static bool IsIpInRange(string ip, string range)
        {
            // range might be 255.255.*.* or 1.2.3.0-1.2.3.255
            if (range.Contains("*")) // a.b.*.* format
            {
                // Just convert to A-B format by setting * to 0 for A and 255 for B
                var lower = range.ReplaceFirst("*", "0");
                var upper = range.ReplaceFirst("*", "255");
                range = $"{lower}-{upper}";
            }

            if (range.Contains("-")) // A-B format
            {
                var split = range.Split('-');
                var lower = split[0];
                var upper = split[1];

                // Get the lower IP address bytes
                var lowerBytes = lower.ToIntIpAddress();
                
                // Get the upper IP address bytes
                var upperBytes = upper.ToIntIpAddress();

                // Get the supplied IP address bytes
                var testBytes = ip.ToIntIpAddress();

                return ((testBytes >= lowerBytes) && (testBytes <= upperBytes));
            }

            return false;
        }

        /// <summary>
        /// Determines if the given address is in any of the ip ranges in the given list.
        /// </summary>
        /// <param name="ip">The IP address</param>
        /// <param name="ranges">The ranges</param>
        /// <returns>true if it's in any range</returns>
        public static bool IsIpInRangeList(string ip, string[] ranges)
        {
            foreach (var range in ranges)
            {
                if (IsIpInRange(ip, range))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the given IP is in the Netmask notation like shown:
        /// 
        /// var ip = "127.0.0.1";
        /// var mask = "127.0.0.0/255.255.255.0";
        /// 
        /// var isInNetmask = NetworkingGlobal.IsIpInNetmask(ip, mask);
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <param name="netmask">Network Mask</param>
        /// <returns>true if the IP is in the network mask</returns>
        public static bool IsIpInNetmask(string ip, string netmask)
        {
            if (netmask.Contains("/"))
            {
                var split = netmask.Split('/');
                var range = split[0];
                var mask = split[1];

                if (mask.Contains("."))
                {
                    // netmask is a
                    // a.b.c.d/mask
                    mask = mask.Replace("*", "0");

                    // Get the mask bytes
                    var maskBytes = mask.ToIntIpAddress();
                    
                    // Get the supplied IP address bytes
                    var testBytes = ip.ToIntIpAddress();

                    // Get the range bytes
                    var rangeBytes = range.ToIntIpAddress();

                    return ((testBytes & maskBytes) == (rangeBytes & maskBytes));
                } 
            }

            return false;
        }

        /// <summary>
        /// Determines if the given address is in any of the ip network masks in the given list.
        /// </summary>
        /// <param name="ip">The IP address</param>
        /// <param name="netmasks">The network masks</param>
        /// <returns>true if it's in any network mask</returns>
        public static bool IsIpInNetmaskList(string ip, string[] netmasks)
        {
            foreach (var netmask in netmasks)
            {
                if (IsIpInNetmask(ip, netmask))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the given ip is in the CIDR notation like this:
        ///
        /// var ip = "127.0.0.1";
        /// var cidr = "127.0.0.0/8";
        ///
        /// var isInCidr = NetworkingGlobal.IsIpInCidr(ip, cidr);
        /// </summary>
        /// <param name="ip">IP Address</param>
        /// <param name="cidr">CIDR</param>
        /// <returns>true if the IP is in the CIDR</returns>
        public static bool IsIpInCidrRange(string ip, string cidr)
        {
            var split = cidr.Split('/');

            if (split.Length < 1)
                return false;

            // Mask is technically optional, if not supplied, assume /32
            var subnet = split[0];
            var mask = split.Length > 1 ? int.Parse(split[1]) : 32;

            // Get the ip bytes
            var ipBytes = ip.ToIntIpAddress();

            // Get the mask bytes
            var maskBytes = -1 << (32 - mask);

            // Get the subnet bytes
            var subnetBytes = subnet.ToIntIpAddress();

            // nb: in case the supplied subnet wasn't correctly aligned
            subnetBytes &= maskBytes;

            // Perform test
            return (ipBytes & maskBytes) == subnetBytes;
        }

        /// <summary>
        /// Determines if the given address is in any of the ip CIDR ranges in the given list.
        /// </summary>
        /// <param name="ip">The IP address</param>
        /// <param name="cidrs">The CIDR ranges</param>
        /// <returns>true if it's in any CIDR range</returns>
        public static bool IsIpInCidrRangeList(string ip, string[] cidrs)
        {
            foreach (var cidr in cidrs)
            {
                if (IsIpInCidrRange(ip, cidr))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if the given address is in the given CIDR, Range or Netmask list.
        /// </summary>
        /// <param name="ip">The IP address</param>
        /// <param name="cidrs">The CIDR ranges</param>
        /// <returns>true if it's in any CIDR range</returns>
        public static bool IsIpInCidrNetmaskOrRangeList(string ip, string[] cidrs)
        {
            if (IsIpInCidrRangeList(ip, cidrs) || IsIpInNetmaskList(ip, cidrs) || IsIpInRangeList(ip, cidrs))
                return true;

            return false;
        }


        public static long GetLocalIpAsInt() => GetLocalIp().ToIntIpAddress();
        public static long ToInt(string ip)
        {
            // careful of sign extension: convert to uint first;
            // unsigned NetworkToHostOrder ought to be provided.
            return (uint)IPAddress.NetworkToHostOrder(
                 (int)IPAddress.Parse(ip).Address);
        }
        public static string ToAddress(long ip) => IPAddress.Parse(ip.ToString()).ToString();
    }
}
