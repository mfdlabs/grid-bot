using MFDLabs.Diagnostics;
using MFDLabs.Grid.Deployer.Tooling;
using Microsoft.Win32;
using System;
using System.Diagnostics;

namespace MFDLabs.Grid.Deployer
{
    class Program
    {
        public static void Main()
        {
            OpenGridServer();
        }

        private static void OpenGridServer()
        {
            ConsoleExtended.WriteTitle("Trying to get the grid server's path...");

            var gridServicePath = Registry.GetValue(
                global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerRegistryKeyName,
                global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerRegistryValueName,
                null
            );

            if (gridServicePath == null)
            {
                ConsoleExtended.WriteTitle("The grid server is not installed on this machine.");
                throw new ApplicationException("The grid server is not installed on this system!");
            }

            ConsoleExtended.WriteTitle("Got grid server path '{0}'.", gridServicePath);
            ConsoleExtended.WriteTitle("Trying to launch web server...");

            LaunchWebServer();

            if (!NetTools.IsServiceAvailable(
                global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerLookupHost,
                global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerLookupPort,
                0
            ))
            {
                ConsoleExtended.WriteTitle("Grid server was not running, try to launch.");

                var gridServerCommand = string.Format(
                    "{0}{1}",
                    gridServicePath,
                    global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerExecutableName
                );

                ConsoleExtended.WriteTitle("Got grid server command '{0}'.", gridServerCommand);

                var psi = new ProcessStartInfo
                {
                    FileName = gridServerCommand,
                    Arguments = global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerExecutableLaunchArguments,
                    WindowStyle = ProcessWindowStyle.Maximized
                };

                if (SystemGlobal.Singleton.ContextIsAdministrator())
                {
                    psi.Verb = "runas";
                }

                var proc = new Process
                {
                    StartInfo = psi
                };

                proc.Start();


                ConsoleExtended.WriteTitle("Successfully launched grid server.");
            }
        }


        private static void LaunchWebServer()
        {
            attempt++;

            ConsoleExtended.WriteTitle("Trying to contact web server at attempt No. {0}", attempt);

            if (NetTools.IsServiceAvailable(
                global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerRemoteServiceName,
                80,
                0
            ))
            {
                ConsoleExtended.WriteTitle("Successfully launched web server at {0} attempts.", attempt);
                return;
            }

            if (!LaunchingWebServer) OpenWebServer();

            LaunchWebServer();
        }

        private static void OpenWebServer()
        {
            ConsoleExtended.WriteTitle("Launching web server with System.Diagnostics.Process{{CMD.EXE}}");

            LaunchingWebServer = true;

            var psi = new ProcessStartInfo
            {
                FileName = "CMD.exe",
                Arguments = string.Format(
                    "/C \"cd {0} && {1}\"",
                    global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerWorkspacePath,
                    global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerWorkspaceCommand
                ),
            };

            if (SystemGlobal.Singleton.ContextIsAdministrator())
            {
                psi.Verb = "runas";
            }

            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();
        }

        static int attempt = 0;

        private static bool LaunchingWebServer = false;
    }
}
