using MFDLabs.Diagnostics;
using MFDLabs.Grid.Deployer.Tooling;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

// TODO: Check the response of LMS so we aren't getting an IIS server for any reason !!

namespace MFDLabs.Grid.Deployer
{
    class Program
    {
        public static void Main(string[] args)
        {
            OpenGridServer(args);
        }

        private static void OpenGridServer(string[] args)
        {
            bool onlyWebServer = false;
            bool onlyGridServer = false;
            int port = 0;
            if (args.Length > 0)
            {
                var option = args[0].Substring(1).ToLower();
                if (option == "onlyweb") onlyWebServer = true;
                if (option == "onlygrid")
                {
                    onlyGridServer = true;
                    if (args.Length > 1) port = Int32.Parse(args[1].Trim().ToLowerInvariant());
                }
            }


            if (onlyGridServer) ConsoleExtended.WriteTitle("Only launching grid server...");
            if (onlyWebServer) ConsoleExtended.WriteTitle("Only launching web server...");

            if (!onlyGridServer)
            {

                ConsoleExtended.WriteTitle("Trying to launch web server...");
                ConsoleExtended.WriteTitle("Checking the existance of the web server at '{0}'", global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerWorkspacePath);

                if (!Directory.Exists(global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerWorkspacePath))
                {
                    ConsoleExtended.WriteTitle("Unable to launch the web server because it could not be found at the path: '{0}'", global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerWorkspacePath);
                    Environment.Exit(1);
                    return;
                }

                ConsoleExtended.WriteTitle("Found web server at '{0}', try launch now.", global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerWorkspacePath);

                LaunchWebServer();
            }

            ConsoleExtended.WriteTitle("Trying to get the grid server's path...");

            if (!onlyWebServer)
            {

                var gridServicePath = Registry.GetValue(
                    global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerRegistryKeyName,
                    global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerRegistryValueName,
                    null
                );

                if (gridServicePath == null)
                {
                    ConsoleExtended.WriteTitle("The grid server is not installed on this machine.");
                    Environment.Exit(1);
                    return;
                }

                ConsoleExtended.WriteTitle("Got grid server path '{0}'.", gridServicePath);

                if (!NetTools.IsServiceAvailable(
                    global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerLookupHost,
                    port != 0 ? port : global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerLookupPort,
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
                        Arguments = port != 0 ? $"{port} -Console -Verbose" : global::MFDLabs.Grid.Deployer.Properties.Settings.Default.GridServerExecutableLaunchArguments,
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
                    Environment.Exit(proc.Id);
                }
            }
        }


        private static void LaunchWebServer()
        {
            attempt++;

            ConsoleExtended.WriteTitle("Trying to contact web server at attempt No. {0}", attempt);

            if (attempt > global::MFDLabs.Grid.Deployer.Properties.Settings.Default.MaxAttemptsToLaunchWebServer)
            {
                ConsoleExtended.WriteTitle("Max attempts exceeded when trying to launch the web server.");
                Environment.Exit(1);
                return;
            }

            if (NetTools.IsServiceAvailable(
                global::MFDLabs.Grid.Deployer.Properties.Settings.Default.WebServerLookupHost,
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
