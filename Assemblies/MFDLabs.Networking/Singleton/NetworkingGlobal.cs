using System;
using System.Net;
using System.Net.Sockets;
using MFDLabs.Abstractions;
using MFDLabs.Networking.Extensions;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Networking
{
    public sealed class NetworkingGlobal : SingletonBase<NetworkingGlobal>
    {
        public string GenerateUUIDV4()
        {
            return Guid.NewGuid().ToString();
        }

        public string LocalIP => GetLocalIPAsInt().ToString();

        public string GetLocalIP()
        {
            if (!global::MFDLabs.Networking.Properties.Settings.Default.LocalIPOverride.IsNullWhiteSpaceOrEmpty()) return global::MFDLabs.Networking.Properties.Settings.Default.LocalIPOverride;

            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "0.0.0.0";
        }

        public long GetLocalIPAsInt()
        {
            return GetLocalIP().ToIntIpAddress();
        }

        public long ToInt(string ip)
        {
            // careful of sign extension: convert to uint first;
            // unsigned NetworkToHostOrder ought to be provided.
            return (long)(uint)IPAddress.NetworkToHostOrder(
                 (int)IPAddress.Parse(ip).Address);
        }

        public string ToAddress(long ip)
        {
            return IPAddress.Parse(ip.ToString()).ToString();
        }
    }
}
