using MFDLabs.Abstractions;
using MFDLabs.Diagnostics;
using MFDLabs.Diagnostics.Extensions;
using MFDLabs.Logging;
using System;
using System.Diagnostics;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class SystemUtility : SingletonBase<SystemUtility>
    {
        private static readonly object _GridLock = new object();

        private bool _runningOpenJob = false;

        public TimeSpan OpenGridServer()
        {
            if (!_runningOpenJob)
            {
                _runningOpenJob = true;
                lock (_GridLock)
                {
                    var sw = Stopwatch.StartNew();
                    SystemLogger.Singleton.Log("Try open Grid Server");
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = Settings.Singleton.GridServerDeployerExecutableName,

                        };


                        if (SystemGlobal.Singleton.ContextIsAdministrator())
                        {
                            psi.Verb = "runas";
                        }

                        if (!Settings.Singleton.GridServerDeployerShouldShowLauncherWindow)
                        {
                            psi.UseShellExecute = false;
                            psi.CreateNoWindow = true;
                            psi.WindowStyle = ProcessWindowStyle.Hidden;
                        }

                        var proc = new Process
                        {
                            StartInfo = psi
                        };

                        proc.Start();
                        proc.WaitForExit();

                        SystemLogger.Singleton.Info(
                            "Successfully opened Grid Server via {0}",
                            Settings.Singleton.GridServerDeployerExecutableName
                        );
                    }
                    catch (Exception ex)
                    {
                        SystemLogger.Singleton.Error(ex);
                    }
                    finally
                    {
                        SystemLogger.Singleton.Debug(
                            "Took {0}s to open Grid Server via {1}",
                            sw.Elapsed.TotalSeconds.ToString("f7"),
                            Settings.Singleton.GridServerDeployerExecutableName
                        );
                        sw.Stop();
                        _runningOpenJob = false;
                    }
                    return sw.Elapsed;
                }
            }
            return TimeSpan.Zero;
        }

        public void KillGridServer()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/f /t /im rccservice.exe"
            };

            if (Settings.Singleton.HideProcessWindows)
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

        public void KillServer()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = "/FI \"WindowTitle eq npm run Start-Main-Job\" /t /f"
            };

            if (Settings.Singleton.HideProcessWindows)
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

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public void KillServerSafe()
        {
            SystemLogger.Singleton.Log("Trying to close Backend server.");

            if (!ProcessHelper.GetProcessByWindowTitle(_GlobalServerJobSignature, out var server))
            {
                SystemLogger.Singleton.Warning("Backend server is not running, ignoring...");
                return;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && server.IsElevated())
            {
                // This is quite useless I think
                SystemLogger.Singleton.Warning("Backend server is running on a higher context than the current process, ignoring...");
                return;
            }

            KillServer();

            SystemLogger.Singleton.Info("Successfully closed backend Server.");
        }

        /// <summary>
        /// "Safe" because it checks if the process exists first.
        /// </summary>
        public void KillGridServerSafe()
        {
            SystemLogger.Singleton.Log("Trying to close GridServer.");

            if (!ProcessHelper.GetProcessByName(_GridServerSignature, out var server))
            {
                SystemLogger.Singleton.Warning("GridServer not running, ignoring...");
                return;
            }

            if (!SystemGlobal.Singleton.ContextIsAdministrator() && server.IsElevated())
            {
                SystemLogger.Singleton.Warning("GridServer is running on a higher context than the current process, ignoring...");
                return;
            }

            KillGridServer();

            SystemLogger.Singleton.Info("Successfully closed GridServer.");
        }

        private const string _GridServerSignature = "rccservice";
        private const string _GlobalServerJobSignature = "npm run Start-Main-Job";
    }
}
