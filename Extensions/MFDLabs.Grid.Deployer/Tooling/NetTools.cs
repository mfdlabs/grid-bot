using MFDLabs.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MFDLabs.Grid.Deployer.Tooling
{
    internal class NetTools
    {
        static internal bool IsServiceAvailableHttp(string host, int port, int retrycount, out bool upButWrongText, string path = "/", string healthyText = "OK")
        {
            upButWrongText = false;
            bool bAvailable = false;
            int waitcount = 0;
            var kind = port == 443 ? "https" : "http";
            var url = $"{kind}://{host}:{port}{path}";
            while (!bAvailable)
            {
                WebClient http = new WebClient();
                http.Headers.Add("User-Agent", $"MFDLABS/WebServerHealthCheck+MFDLabs.Grid.Deployer::{url}->{healthyText}::{SystemGlobal.Singleton.AssemblyVersion}");
                try
                {
                    var result = http.DownloadString(url);
                    bAvailable = true;
                    if (result == healthyText)
                    {
                        upButWrongText = false;
                        break;
                    }
                    upButWrongText = true;
                    break;
                }
                catch (WebException x)
                {
                    if (x.Response != null)
                    {
                        bAvailable = true;
                        upButWrongText = true;
                        break;
                    }

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

        static internal bool IsServiceAvailableTcp(string host, int port, int retrycount)
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
