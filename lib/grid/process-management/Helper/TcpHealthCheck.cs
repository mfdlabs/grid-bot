namespace Grid;

using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

/// <summary>
/// Interface for checking if a TCP port is available.
/// </summary>
internal static class TcpHealthCheck
{
    /// <summary>
    /// Checks if a TCP port is available.
    /// </summary>
    /// <param name="hostname">The hostname to check. Defaults to localhost.</param>
    /// <param name="port">The port to check. Defaults to 53640.</param>
    /// <param name="retryCount">The number of times to retry the check. Defaults to 3.</param>
    /// <param name="timeout">The timeout for the check in milliseconds. Defaults to 1000.</param>
    /// <returns>True if the port is available, false otherwise.</returns>
    public static bool IsAlive(string hostname = "localhost", int port = 53640, int retryCount = 3, int timeout = 1000)
    {
        // busy wait untill our service is ready to accept connections
        var bAvailable = false;
        var waitcount = 0;
        while (!bAvailable)
        {
            using var tcp = new TcpClient();
            try
            {
                tcp.Connect(hostname, port);
                tcp.Close();
                bAvailable = true;
            }
            catch (SocketException)
            {
                if (++waitcount > retryCount)
                {
                    bAvailable = false;
                    break;
                }
                
                Task.Delay(timeout).Wait();
            }
        }

        return bAvailable;
    }

    public static bool GetProcessByHostnameAndPort(string hostname, int port, out Process process)
    {
        process = null;

        if (!IsAlive(hostname, port, 0, 100))
            return false;

        var row = ManagedIpHelper.GetExtendedTcpTable(true)
            .FirstOrDefault(p => p.LocalEndPoint.Address.ToString() == hostname && p.LocalEndPoint.Port == port);

        if (row != null)
        {
            try {
                process = Process.GetProcessById((int)row.ProcessId);

                return true;
            }
            catch { }
        }

        return false;
    }
}
