using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using MFDLabs.Diagnostics;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

#if NETFRAMEWORK
using Microsoft.Win32;
#endif

namespace MFDLabs.Grid
{
    public static class GridDeployer
    {
        private static string ConstructHealthCheckUserAgent(string url, string healthyText)
            => $"MFDLABS/WebServerHealthCheck+MFDLabs.Grid.Deployer::{url}->{healthyText}::{SystemGlobal.AssemblyVersion}";


        private static string GetGridServerLaunchPath()
        {
#if NETFRAMEWORK
            var gridServicePath = Registry.GetValue(
                global::MFDLabs.Grid.Properties.Settings.Default.GridServerRegistryKeyName,
                global::MFDLabs.Grid.Properties.Settings.Default.GridServerRegistryValueName,
                null
            );
#else
            var gridServicePath = Environment.GetEnvironmentVariable("GRID_SERVER_PATH");
#endif

            if (gridServicePath == null) 
                throw new InvalidOperationException(CouldNotFindGridServer);

            return (string)gridServicePath;
        }

        private static bool IsServiceAvailableTcp(string host, int port, int retrycount)
        {
            // busy wait untill our service is ready to accept connections
            var bAvailable = false;
            var waitcount = 0;
            while (!bAvailable)
            {
                var tcp = new TcpClient();
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

        private static bool IsServiceAliveHttp(
            string host,
            int port,
            int retryCount,
            out bool aliveButStatusCheckInvalid,
            string healthCheckPath = "/",
            string healthyText = "OK",
            bool isSsl = false
        )
        {
            aliveButStatusCheckInvalid = false;
            var bAvailable = false;
            var waitcount = 0;
            var kind = isSsl ? "https" : "http";
            var url = $"{kind}://{host}:{port}{healthCheckPath}";

            while (!bAvailable)
            {
                var webClient = new WebClient();
                webClient.Headers.Add("User-Agent", ConstructHealthCheckUserAgent(url, healthyText));
                try
                {
                    var result = webClient.DownloadString(url);
                    bAvailable = true;
                    aliveButStatusCheckInvalid = result != healthyText;
                    break;
                }
                catch (WebException x)
                {
                    if (x.Response != null)
                    {
                        bAvailable = true;
                        aliveButStatusCheckInvalid = true;
                        break;
                    }

                    if (++waitcount > retryCount)
                    {
                        bAvailable = false;
                        break;
                    }
                    Thread.Sleep(1000);//long sleep causes slow responses
                }
            }

            return bAvailable;
        }

        public static bool WebServerIsAvailable(out bool aliveButBadCheck)
        {
            aliveButBadCheck = false;

            return (ProcessHelper.GetProcessByWindowTitle(GlobalServerJobSignature, out _) ||
                    ProcessHelper.GetProcessByWindowTitle(GlobalQuickServerJobSignature, out _)) &&
                    IsServiceAliveHttp(
                        global::MFDLabs.Grid.Properties.Settings.Default.WebServerLookupHost,
                        80,
                        0,
                        out aliveButBadCheck,
                        "/checkhealth",
                        global::MFDLabs.Grid.Properties.Settings.Default.WebServerHealthCheckExpectedResponseText,
                        false
                    );
        }

        public enum WebServerDeploymentStatus
        {
            MaxAttemptsExceeded,
            UpButIncorrectHealthCheckText,
            Success
        }



        private static void InvokeDeploymentOnWebServer()
        {
            if (_runningWebServerLaunch) return;

            _runningWebServerLaunch = true;


            var command = "npm start";

            if (global::MFDLabs.Grid.Properties.Settings.Default.WebServerBuildBeforeRun)
                command = "npm run Build-And-Run";

            var psi = new ProcessStartInfo
            {
                FileName = "CMD.exe",
                Arguments =
                    $"/C \"cd {(global::MFDLabs.Grid.Properties.Settings.Default.WebServerWorkspacePath)} && " +
                    $"{command}\"",
            };

            if (SystemGlobal.ContextIsAdministrator())
            {
                psi.Verb = "runas";
            }

            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();
        }

        private static void CheckWorkspace()
        {
            if (global::MFDLabs.Grid.Properties.Settings.Default.WebServerWorkspacePath.IsNullOrEmpty())
                throw new InvalidOperationException("Cannot open web server if the workspace is not set!");

            if (!Directory.Exists(global::MFDLabs.Grid.Properties.Settings.Default.WebServerWorkspacePath))
                throw new InvalidOperationException(CouldNotFindWebServer);
        }

        public static int BootstrapLaunch(string hostName = "localhost", int port = 53640, int maxAttempts = 15)
        {
            LaunchWebServer(maxAttempts);
            return LaunchGridServer(hostName, port);
        }

        public static int LaunchGridServer(string hostName = "localhost", int port = 53640)
        {
            if (!IsServiceAvailableTcp(hostName, port, 0))
            {
                var path = GetGridServerLaunchPath();

                var command = $"{path}{(global::MFDLabs.Grid.Properties.Settings.Default.GridServerExecutableName)}";

                var psi = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = $"{port} -Console -Verbose",
                    WindowStyle = ProcessWindowStyle.Maximized
                };

                if (SystemGlobal.ContextIsAdministrator())
                {
                    psi.Verb = "runas";
                }

                var proc = new Process
                {
                    StartInfo = psi
                };

                proc.Start();


                return proc.Id;
            }

            return 0;
        }

        public static void LaunchWebServer(int maxAttempts = 15)
        {
            CheckWorkspace();

            if (WebServerIsAvailable(out var aliveButBadCheck))
            {
                if (aliveButBadCheck)
                    throw new InvalidOperationException(WebServerUpButBadHealthCheckText);

                return;
            }

            for (int attempt = 0; attempt < maxAttempts; ++attempt)
            {
                SystemLogger.Singleton.Info("Trying to contact web server at attempt No. {0}", attempt);

                if (WebServerIsAvailable(out aliveButBadCheck))
                {
                    if (aliveButBadCheck) 
                        throw new InvalidOperationException(WebServerUpButBadHealthCheckText);

                    _runningWebServerLaunch = false;

                    return;
                }

                InvokeDeploymentOnWebServer();
            }

            throw new TimeoutException(MaxAttemptsExceededWhenLaunchingWebServer);
        }

        public static WebServerDeploymentStatus LaunchWebServerSafe(int maxAttempts = 15)
        {
            CheckWorkspace();

            if (WebServerIsAvailable(out var aliveButBadCheck))
            {
                if (aliveButBadCheck) return WebServerDeploymentStatus.UpButIncorrectHealthCheckText;
                return WebServerDeploymentStatus.Success;
            }

            for (int attempt = 0; attempt < maxAttempts; ++attempt)
            {
                SystemLogger.Singleton.Info("Trying to contact web server at attempt No. {0}", attempt);

                if (WebServerIsAvailable(out aliveButBadCheck))
                {
                    if (aliveButBadCheck) return WebServerDeploymentStatus.UpButIncorrectHealthCheckText;

                    _runningWebServerLaunch = false;

                    return WebServerDeploymentStatus.Success;
                }

                InvokeDeploymentOnWebServer();
            }

            return WebServerDeploymentStatus.MaxAttemptsExceeded;
        }

        private static bool _runningWebServerLaunch = false;

        private const string GlobalServerJobSignature = "npm run Start-Main-Job";
        private const string GlobalQuickServerJobSignature = "npm start";
        private const string WebServerUpButBadHealthCheckText = "The web server was up, but it didn't return the OK response the grid deployer expected, most likely there is another service hogging the healthcheck port.";
        private const string MaxAttemptsExceededWhenLaunchingWebServer = "The grid deployer exceeded it's maximum attempts when trying to launch the web server.";
        private const string CouldNotFindGridServer = "The grid deployer tried to launch a grid server but it couldn't find the grid server's path in Win32 registry.";
        private const string CouldNotFindWebServer = "The grid deployer tried to launch the web server, but there was no web server at the specified path.";
    }
}
