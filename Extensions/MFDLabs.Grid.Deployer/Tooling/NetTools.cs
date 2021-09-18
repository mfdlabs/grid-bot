using System.Net.Sockets;
using System.Threading;

namespace MFDLabs.Grid.Deployer.Tooling
{
    internal class NetTools
    {
        static internal bool IsServiceAvailable(string host, int port, int retrycount)
        {
            // busy wait untill our service is ready to accept connections
            bool bAvailable = false;
            int waitcount = 0;
            while (!bAvailable)
            {
                TcpClient tcp = new TcpClient();
                try
                {
                    tcp.Connect(host, port);
                    tcp.Close();
                    bAvailable = true;
                }
                catch (SocketException)
                {
                    if (++waitcount > retrycount)
                    {
                        bAvailable = false;
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }

            return bAvailable;
        }
    }
}
