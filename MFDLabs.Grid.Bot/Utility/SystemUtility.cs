using System;
using System.Diagnostics;
using MFDLabs.Diagnostics;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Logging;

// ReSharper disable UnusedTupleComponentInReturnValue

namespace MFDLabs.Grid.Bot.Utility
{
    public static class SystemUtility
    {
        private static readonly object GridLock = new object();

        private static bool _runningOpenJob;

        private static (TimeSpan, int) OpenGridServer(bool onlyWebServer = false, bool onlyGridServer = false, int gridServerPort = 0)
        {
            var sw = Stopwatch.StartNew();
            if (onlyWebServer)
                SystemLogger.Singleton.Log("Try open Web Server");
            switch (onlyGridServer)
            {
                case true:
                    SystemLogger.Singleton.Log("Try open Grid Server");
                    break;
                case false when !onlyWebServer:
                    SystemLogger.Singleton.Log("Try open Grid and Web Server");
                    break;
            }

            int procId;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerExecutableName,

                };

                if (SystemGlobal.ContextIsAdministrator())
                {
                    psi.Verb = "runas";
                }

                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerShouldShowLauncherWindow)
                {
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                }

                if (onlyWebServer)
                {
                    psi.Arguments = "-onlyweb";
                }

                if (onlyGridServer)
                {
                    psi.Arguments = "-onlygrid";
                }

                if (gridServerPort != 0)
                {
                    psi.Arguments += $" {gridServerPort}";
                }

                var proc = new Process
                {
                    StartInfo = psi
                };

                proc.Start();
                proc.WaitForExit();

                GridDeployerUtility.CheckHResult(proc.ExitCode);

                procId = proc.ExitCode;

                SystemLogger.Singleton.Info(
                    "Successfully opened {0} Server via {0}",
                    onlyWebServer ? "Web" : onlyGridServer ? "Grid" : "Web and Grid",
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerExecutableName
                );
            }
            finally
            {
                SystemLogger.Singleton.Debug(
                    "Took {0}s to open Grid Server via {1}",
                    sw.Elapsed.TotalSeconds.ToString("f7"),
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerExecutableName
                );
                sw.Stop();
                _runningOpenJob = false;
            }
            return (sw.Elapsed, onlyGridServer ? procId : 0);
        }

        public static (TimeSpan, int) OpenGridServerSafe(bool onlyWebServer = false, bool onlyGridServer = false, int gridServerPort = 0)
        {
            if (_runningOpenJob) return (TimeSpan.Zero, 0);
            
            _runningOpenJob = true;
            
            lock (GridLock) return OpenGridServer(onlyWebServer, onlyGridServer, gridServerPort);
        }

        /// <summary>
        /// Safe get grid server by port
        /// </summary>
        /// <param name="port">Port</param>
        /// well fuck the tcp port check because it is slower than jakob getting out of the toilet
        public static (TimeSpan, int) OpenGridServerInstance(int port = 0, bool @unsafe = false)
        {
            // this is so fucking slow, please just use win32 native, don't hook fucking netstat
            //if (!ProcessHelper.GetProcessByTcpPortAndName(_GridServerSignature, port == 0 ? 53640 : port, out var process))
            return !@unsafe ? OpenGridServerSafe(false, true, port) : OpenGridServer(false, true, port);
            //return (TimeSpan.Zero, process);
        }

        /// <summary>
        /// Safe open of web server
        /// </summary>
        public static TimeSpan OpenWebServerIfNotOpen() => !WebServerIsAvailable() ? OpenGridServerSafe(true).Item1 : TimeSpan.Zero;

        public static bool WebServerIsAvailable() => ProcessHelper.GetProcessByWindowTitle(GlobalServerJobSignature, out _);

        public static void KillAllProcessByName(string name)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/f /t /im {name}"
            };

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.HideProcessWindows)
            {
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }

            if (SystemGlobal.ContextIsAdministrator())
            {
                psi.Verb = "runas";
            }

            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();
            proc.WaitForExit();
        }

        public static void KillProcessByPid(int pid)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/f /t /PID {pid}"
            };

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.HideProcessWindows)
            {
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }

            if (SystemGlobal.ContextIsAdministrator())
            {
                psi.Verb = "runas";
            }

            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();
            proc.WaitForExit();
        }

        public static bool KillProcessByPidSafe(int pid)
        {
            if (!ProcessHelper.GetProcessById(pid, out var pr))
            {
                SystemLogger.Singleton.Warning("The process '{0}' is not running, ignoring...", pid);
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator() && pr.IsElevated())
            {
                SystemLogger.Singleton.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", pid);
                return false;
            }

            KillProcessByPid(pid);

            SystemLogger.Singleton.Info("Successfully closed process '{0}'.", pid);
            return true;
        }

        public static bool KillAllProcessByNameSafe(string name)
        {
            if (!ProcessHelper.GetProcessByName(name.ToLower().Replace(".exe", ""), out var pr))
            {
                SystemLogger.Singleton.Warning("The process '{0}' is not running, ignoring...", name);
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator() && pr.IsElevated())
            {
                SystemLogger.Singleton.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", name);
                return false;
            }

            KillAllProcessByName(name);

            SystemLogger.Singleton.Info("Successfully closed process '{0}'.", name);
            return true;
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public static bool KillServerSafe()
        {
            SystemLogger.Singleton.Log("Trying to close Backend server.");

            if (!ProcessHelper.GetProcessByWindowTitle(GlobalServerJobSignature, out var server))
            {
                SystemLogger.Singleton.Warning("Backend server is not running, ignoring...");
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator() && server.IsElevated())
            {
                // This is quite useless I think
                SystemLogger.Singleton.Warning("Backend server is running on a higher context than the current process, ignoring...");
                return false;
            }

            KillProcessByPidSafe(server.Id);

            SystemLogger.Singleton.Info("Successfully closed backend Server.");
            return true;
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public static bool KillAllDeployersSafe()
        {
            SystemLogger.Singleton.Log("Trying to close all open grid deployer instances.");

            if (!ProcessHelper.GetProcessByWindowTitle(GridDeployerSignature, out var deployer))
            {
                SystemLogger.Singleton.Warning("There are no grid deployers running, ignoring...");
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator() && deployer.IsElevated())
            {
                // This is quite useless I think
                SystemLogger.Singleton.Warning("The grid deployer we caught is running on a different context than us, ignoring...");
                return false;
            }

            KillAllProcessByNameSafe(GridDeployerSignatureExe);

            SystemLogger.Singleton.Info("Successfully closed all grid deployer instances.");
            return true;
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public static bool KillAllGridServersSafe()
        {
            SystemLogger.Singleton.Log("Trying to close all open grid server instances.");

            if (!ProcessHelper.GetProcessByName(GridServerSignature, out var server))
            {
                SystemLogger.Singleton.Warning("There are no grid servers running, ignoring...");
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator() && server.IsElevated())
            {
                SystemLogger.Singleton.Warning("The grid server we caught is running on a different context than us, ignoring...");
                return false;
            }

            KillAllProcessByNameSafe(GridServerSignatureExe);

            SystemLogger.Singleton.Info("Successfully closed all grid server instances.");

            return true;
        }

        private const string GridServerSignature = "rccservice";
        private const string GridServerSignatureExe = "rccservice.exe";
        private const string GridDeployerSignature = "mfdlabs.grid.deployer";
        private const string GridDeployerSignatureExe = "mfdlabs.grid.deployer.exe";
        private const string GlobalServerJobSignature = "npm run Start-Main-Job";
    }
}
