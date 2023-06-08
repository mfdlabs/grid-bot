﻿using System;
using System.Diagnostics;
using MFDLabs.Diagnostics;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Logging;

// ReSharper disable UnusedTupleComponentInReturnValue

namespace MFDLabs.Grid
{
    public static class GridProcessHelper
    {
        private static readonly object GridLock = new();

        private static bool _runningOpenJob;

        /// <summary>
        /// Special case here, opens either a grid server or webserver, or both.
        /// </summary>
        /// <param name="onlyWebServer"></param>
        /// <param name="onlyGridServer"></param>
        /// <param name="gridServerPort"></param>
        /// <returns></returns>
        private static (TimeSpan elapsed, int procId) OpenServer(bool onlyWebServer = false, bool onlyGridServer = false, int gridServerPort = 0)
        {
            var sw = Stopwatch.StartNew();
            if (onlyWebServer)
                Logger.Singleton.Log("Try open Web Server");
            switch (onlyGridServer)
            {
                case true:
                    Logger.Singleton.Log("Try open Grid Server");
                    break;
                case false when !onlyWebServer:
                    Logger.Singleton.Log("Try open Grid and Web Server");
                    break;
            }

            int procId = 0;
            try
            {
                if (onlyGridServer) procId = GridDeployer.LaunchGridServer("localhost", gridServerPort != 0 ? gridServerPort : 53640);
                if (onlyWebServer) GridDeployer.LaunchWebServer(15);

                Logger.Singleton.Info(
                    "Successfully opened '{0}' Server via Memory Roblox Grid Server Deployer",
                    onlyWebServer ? "Web" : onlyGridServer ? "Grid" : "Web and Grid"
                );
            }
            finally
            {
                Logger.Singleton.Debug(
                    "Took {0}s to open Grid Server via Memory Roblox Grid Server Deployer",
                    sw.Elapsed.TotalSeconds.ToString("f7")
                );
                sw.Stop();
                _runningOpenJob = false;
            }
            return (sw.Elapsed, onlyGridServer ? procId : 0);
        }

        public static (TimeSpan elapsed, int procId) OpenServerSafe(bool onlyWebServer = false, bool onlyGridServer = false, int gridServerPort = 0)
        {
            if (_runningOpenJob) return (TimeSpan.Zero, 0);

            _runningOpenJob = true;

            lock (GridLock) return OpenServer(onlyWebServer, onlyGridServer, gridServerPort);
        }

        /// <summary>
        /// Safe get grid server by port
        /// </summary>
        /// <param name="port">Port</param>
        /// <param name="unsafe"></param>
        public static (TimeSpan elapsed, int procId) OpenGridServerInstance(int port = 0, bool @unsafe = false)
        {
            // this is so fucking slow, please just use win32 native, don't hook fucking netstat
            //if (!ProcessHelper.GetProcessByTcpPortAndName(_GridServerSignature, port == 0 ? 53640 : port, out var process))
            return !@unsafe ? OpenServerSafe(false, true, port) : OpenServer(false, true, port);
            //return (TimeSpan.Zero, process);
        }

        /// <summary>
        /// Safe open of web server
        /// </summary>
        public static TimeSpan OpenWebServerIfNotOpen() => !WebServerIsAvailable() ? OpenServerSafe(true).elapsed : TimeSpan.Zero;

        // There will have to be a #if here for unix, because we cannot check if there's a window with a title
        // WIN32: This method is Win32 only
        private static bool WebServerIsAvailable() => ProcessHelper.GetProcessByWindowTitle(GlobalServerJobSignature, out _) || 
                                                      ProcessHelper.GetProcessByWindowTitle(GlobalQuickServerJobSignature, out _);

        private static void KillAllProcessByName(string name)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/f /t /im {name}"
            };

            if (global::MFDLabs.Grid.Properties.Settings.Default.HideProcessWindows)
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

        private static void KillProcessByPid(int pid)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/f /t /PID {pid}"
            };

            if (global::MFDLabs.Grid.Properties.Settings.Default.HideProcessWindows)
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
                Logger.Singleton.Warning("The process '{0}' is not running, ignoring...", pid);
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator()
#if NETFRAMEWORK
                && pr.IsElevated()
#endif
                )
            {
                Logger.Singleton.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", pid);
                return false;
            }

            KillProcessByPid(pid);

            Logger.Singleton.Info("Successfully closed process '{0}'.", pid);
            return true;
        }

        private static void KillAllProcessByNameSafe(string name)
        {
            if (!ProcessHelper.GetProcessByName(name.ToLower().Replace(".exe", ""), out var pr))
            {
                Logger.Singleton.Warning("The process '{0}' is not running, ignoring...", name);
                return;
            }

            if (!SystemGlobal.ContextIsAdministrator()
#if NETFRAMEWORK // for now
                && pr.IsElevated()
#endif

                )
            {
                Logger.Singleton.Warning("The process '{0}' is running on a higher context than the current process, ignoring...", name);
                return;
            }

            KillAllProcessByName(name);

            Logger.Singleton.Info("Successfully closed process '{0}'.", name);
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public static bool KillServerSafe()
        {
            Logger.Singleton.Log("Trying to close Backend server.");

            if (!ProcessHelper.GetProcessByWindowTitle(GlobalServerJobSignature, out var server) && !ProcessHelper.GetProcessByWindowTitle(GlobalQuickServerJobSignature, out server))
            {
                Logger.Singleton.Warning("Backend server is not running, ignoring...");
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator()
#if NETFRAMEWORK
                && server.IsElevated()
#endif
                )
            {
                // This is quite useless I think
                Logger.Singleton.Warning("Backend server is running on a higher context than the current process, ignoring...");
                return false;
            }

            KillProcessByPidSafe(server.Id);

            Logger.Singleton.Info("Successfully closed backend Server.");
            return true;
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public static bool KillAllGridServersSafe()
        {
            Logger.Singleton.Log("Trying to close all open grid server instances.");

            if (!ProcessHelper.GetProcessByName(GridServerSignature, out var server))
            {
                Logger.Singleton.Warning("There are no grid servers running, ignoring...");
                return false;
            }

            if (!SystemGlobal.ContextIsAdministrator()
#if NETFRAMEWORK
                && server.IsElevated()
#endif
                )
            {
                Logger.Singleton.Warning("The grid server we caught is running on a different context than us, ignoring...");
                return false;
            }

            KillAllProcessByNameSafe(GridServerSignatureExe);

            Logger.Singleton.Info("Successfully closed all grid server instances.");

            return true;
        }

        private const string GridServerSignature = "rccservice";
        private const string GridServerSignatureExe = "rccservice.exe";
        private const string GlobalServerJobSignature = "npm run Start-Main-Job";
        private const string GlobalQuickServerJobSignature = "npm start";
    }
}