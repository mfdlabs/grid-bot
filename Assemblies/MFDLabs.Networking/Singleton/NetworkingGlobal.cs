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
