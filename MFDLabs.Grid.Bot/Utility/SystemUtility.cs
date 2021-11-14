using System;
using System.Diagnostics;
using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class SystemUtility : SingletonBase<SystemUtility>
    {
        private static readonly object _GridLock = new object();

        private bool _runningOpenJob = false;

        public (TimeSpan, int) OpenGridServer(bool onlyWebServer = false, bool onlyGridServer = false, int gridServerPort = 0)
        {
            var sw = Stopwatch.StartNew();
            if (onlyWebServer)
                SystemLogger.Singleton.Log("Try open Web Server");
            if (onlyGridServer)
                SystemLogger.Singleton.Log("Try open Grid Server");
            if (!onlyGridServer && !onlyWebServer)
                SystemLogger.Singleton.Log("Try open Grid and Web Server");
            var procId = 0;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerExecutableName,

                };

                if (SystemGlobal.Singleton.ContextIsAdministrator())
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

                if (proc.ExitCode == 1) throw new ApplicationException($"Unable to open the {(onlyWebServer ? "Web" : onlyGridServer ? "Grid" : "Web and Grid")} server due to an internal exception on the machine '{SystemGlobal.Singleton.GetMachineID()} ({SystemGlobal.Singleton.GetMachineHost()})', please contact a datacenter administrator.");

                procId = proc.ExitCode;

                SystemLogger.Singleton.Info(
                    "Successfully opened {0} Server via {0}",
                    onlyWebServer ? "Web" : onlyGridServer ? "Grid" : "Web and Grid",
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.GridServerDeployerExecutableName
                );
            }
            catch (Exception ex)
            {
                throw ex;
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

        public (TimeSpan, int) OpenGridServerSafe(bool onlyWebServer = false, bool onlyGridServer = false, int gridServerPort = 0)
        {
            if (!_runningOpenJob)
            {
                _runningOpenJob = true;
                lock (_GridLock)
                {
                    return OpenGridServer(onlyWebServer, onlyGridServer, gridServerPort);
                }
            }
            return (TimeSpan.Zero, 0);
        }

        /// <summary>
        /// Safe get grid server by port
        /// </summary>
        /// <param name="port">Port</param>
        /// well fuck the tcp port check because it is slower than jakob getting out of the toilet
        public (TimeSpan, int) OpenGridServerInstance(int port = 0, bool @unsafe = false)
        {
            // this is so fucking slow, please just use win32 native, don't hook fucking netstat
            //if (!ProcessHelper.GetProcessByTcpPortAndName(_GridServerSignature, port == 0 ? 53640 : port, out var process))
            if (!@unsafe)
                return OpenGridServerSafe(false, true, port);
            return OpenGridServer(false, true, port);
            //return (TimeSpan.Zero, process);
        }

        /// <summary>
        /// Safe open of web server
        /// </summary>
        public TimeSpan OpenWebServerIfNotOpen()
        {
            if (!WebServerIsAvailable())
            {
                return OpenGridServerSafe(true).Item1;
            }
            return TimeSpan.Zero;
        }

        public bool WebServerIsAvailable() => ProcessHelper.GetProcessByWindowTitle(_GlobalServerJobSignature, out _);

        public void KillAllProcessByName(string name)
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

            if (SystemGlobal.Singleton.ContextIsAdministrator())
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

        public void KillProcessByPID(int pid)
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

            if (SystemGlobal.Singleton.ContextIsAdministrator())
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

        public bool KillProcessByPIDSafe(int pid)
        {
            if (!ProcessHelper.GetProcessById(pid, out var pr))
            {
                SystemLogger.Singleton.Warning("The process '{0}' is not running, ignoring...", pid);
                return false;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && pr.IsElevated())
            {
                SystemLogger.Singleton.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", pid);
                return false;
            }

            KillProcessByPID(pid);

            SystemLogger.Singleton.Info("Successfully closed process '{0}'.", pid);
            return true;
        }

        public bool KillAllProcessByNameSafe(string name)
        {
            if (!ProcessHelper.GetProcessByName(name.ToLower().Replace(".exe", ""), out var pr))
            {
                SystemLogger.Singleton.Warning("The process '{0}' is not running, ignoring...", name);
                return false;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && pr.IsElevated())
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
        public bool KillServerSafe()
        {
            SystemLogger.Singleton.Log("Trying to close Backend server.");

            if (!ProcessHelper.GetProcessByWindowTitle(_GlobalServerJobSignature, out var server))
            {
                SystemLogger.Singleton.Warning("Backend server is not running, ignoring...");
                return false;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && server.IsElevated())
            {
                // This is quite useless I think
                SystemLogger.Singleton.Warning("Backend server is running on a higher context than the current process, ignoring...");
                return false;
            }

            KillProcessByPIDSafe(server.Id);

            SystemLogger.Singleton.Info("Successfully closed backend Server.");
            return true;
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public bool KillAllDeployersSafe()
        {
            SystemLogger.Singleton.Log("Trying to close all open grid deployer instances.");

            if (!ProcessHelper.GetProcessByWindowTitle(_GridDeployerSignature, out var deployer))
            {
                SystemLogger.Singleton.Warning("There are no grid deployers running, ignoring...");
                return false;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && deployer.IsElevated())
            {
                // This is quite useless I think
                SystemLogger.Singleton.Warning("The grid deployer we caught is running on a different context than us, ignoring...");
                return false;
            }

            KillAllProcessByNameSafe(_GridDeployerSignatureExe);

            SystemLogger.Singleton.Info("Successfully closed all grid deployer instances.");
            return true;
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public bool KillAllGridServersSafe()
        {
            SystemLogger.Singleton.Log("Trying to close all open grid server instances.");

            if (!ProcessHelper.GetProcessByName(_GridServerSignature, out var server))
            {
                SystemLogger.Singleton.Warning("There are no grid servers running, ignoring...");
                return false;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && server.IsElevated())
            {
                SystemLogger.Singleton.Warning("The grid server we caught is running on a different context than us, ignoring...");
                return false;
            }

            KillAllProcessByNameSafe(_GridServerSignatureExe);

            SystemLogger.Singleton.Info("Successfully closed all grid server instances.");

            return true;
        }

        internal const string _GridServerSignature = "rccservice";
        internal const string _GridServerSignatureExe = "rccservice.exe";
        internal const string _GridDeployerSignature = "mfdlabs.grid.deployer";
        internal const string _GridDeployerSignatureExe = "mfdlabs.grid.deployer.exe";
        internal const string _GlobalServerJobSignature = "npm run Start-Main-Job";
    }
}
