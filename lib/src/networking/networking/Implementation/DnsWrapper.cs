namespace Networking;

using System.Net;

/// <inheritdoc cref="IDns"/>
internal class DnsWrapper : IDns
{
    /// <inheritdoc cref="IDns.GetHostName"/>
    public string GetHostName() => Dns.GetHostName();

    /// <inheritdoc cref="IDns.GetHostEntry(string)"/>
    public IPHostEntry GetHostEntry(string hostNameOrAddress) 
        => Dns.GetHostEntry(hostNameOrAddress);
}
