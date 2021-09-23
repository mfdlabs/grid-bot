using MFDLabs.Abstractions;
using MFDLabs.Text.Extensions;
using System;
using System.Net;
using System.Net.Sockets;

namespace MFDLabs.Networking
{
    public sealed class NetworkingGlobal : SingletonBase<NetworkingGlobal>
    {
        public string GenerateUUIDV4()
        {
            return Guid.NewGuid().ToString();
        }

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
    }
}
