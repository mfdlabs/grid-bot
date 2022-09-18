using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.CommandLine;
using System.Threading.Tasks;
using System.CommandLine.Parsing;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using Microsoft.Win32;
using MFDLabs.Wcf;
using MFDLabs.Logging;
using MFDLabs.FileSystem;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Configuration.Logging;
using MFDLabs.Grid.AutoDeployer.Service;

namespace MFDLabs.Grid.AutoDeployer;

internal static class Program
{
    private static readonly Option<bool> PurgeOption = new(new[] { "-purge", "/purge", "--purge" }, "Purge all known deployer info.") { IsRequired = false };

    public static async Task Main(params string[] args)
    {
        Logger.Singleton.LogLevel = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.EnvironmentLogLevel;
        EventLogLogger.Singleton.LogLevel = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.EnvironmentLogLevel;

        ConfigurationLogging.OverrideDefaultConfigurationLogging(
            EventLogLogger.Singleton.Error,
            Logger.Singleton.Warning,
            Logger.NoopSingleton.Info
        );

        // If args has -purge or --purge etc.
        // purge all known deployer info.
        var rootCommand = new RootCommand(
            description: "Polls Github Cloud or Github Enterprise constantly for new releases to deploy to the host machine."
        )
        {
            PurgeOption,
            new Option<bool>(new[] { "-console", "/console", "--console" }, "Launch in console mode") { IsRequired = false },
            new Option<bool>(new[] { "-install", "/install", "--install" }, "Installs the Application as a Windows Daemon") { IsRequired = false },
            new Option<bool>(new[] { "-uninstall", "/uninstall", "--uninstall" }, "Uninstalls the Application if the Windows Daemon exists") { IsRequired = false }
        };
        rootCommand.TreatUnmatchedTokensAsErrors = false;

        rootCommand.SetHandler(Run);

        var arguments = new CommandLineBuilder(rootCommand)
            .UseHelp(int.MaxValue)
            .UseParseDirective()
            .UseSuggestDirective()
            .RegisterWithDotnetSuggest()
            .UseTypoCorrections()
            .UseParseErrorReporting()
            .Build()
            .Parse(args);

        Environment.Exit(await arguments.InvokeAsync());
    }

    private static void Run(InvocationContext context)
    {
        var args = from token in context.ParseResult.Tokens select token.Value;

        if (context.ParseResult.GetValueForOption(PurgeOption))
        {
            Logger.Singleton.Warning("--purge set. Purging deployment files...");

            var deploymentPath = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.DeploymentPath;
            var versioningRegSubKey = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.VersioningRegistrySubKey;

            // If null, just exit and warn.
            if (deploymentPath.IsNullOrEmpty())
            {
                Logger.Singleton.Warning("Deployment path not set, cannot clear deployment directory. Exiting...");
                return;
            }
            if (versioningRegSubKey.IsNullOrEmpty())
            {
                Logger.Singleton.Warning("Version Registry SubKey not set, cannot delete registry key. Exiting...");
                return;
            }

            var purgeDeploymentPath = true;
            var purgeRegistryKey = true;

            if (!Directory.Exists(deploymentPath))
            {
                Logger.Singleton.Warning("Deployment path at '{0}' does not exist or is a file. Ignoring...", deploymentPath);
                purgeDeploymentPath = false;
            }

            if (Registry.LocalMachine.OpenSubKey(versioningRegSubKey) == null)
            {
                Logger.Singleton.Warning("Version Registry SubKey at 'HKLM:{0}' did not exist. Ignoring...", versioningRegSubKey);
                purgeRegistryKey = false;
            }

            if (purgeDeploymentPath)
            {
                foreach (var directory in Directory.EnumerateDirectories(deploymentPath)) // TODO: Match DeploymentId regex?
                {
                    Logger.Singleton.LifecycleEvent("Deleting directory '{0}'...", directory);

                    directory.PollDeletionBlocking(
                        maxAttempts: 2,
                        onFailure: ex =>
                        {
                            if (ex is UnauthorizedAccessException)
                                Logger.Singleton.Warning(
                                    "Could not delete directory because we do not have write access. " +
                                    "Please run this app with elevated permissions or allow the user '{0}\\{1}' " +
                                    "to write to the directory '{2}' and it's sub-directories. Ignoring...",
                                    SystemGlobal.GetMachineId(),
                                    ProcessHelper.GetCurrentUser(),
                                    directory
                                );
                            else
                                Logger.Singleton.Warning("Could not delete directory '{0}' because '{1}'", directory, ex.Message);
                        },
                        onSuccess: () => Logger.Singleton.LifecycleEvent("Successfully deleted directory '{0}'!", directory)
                    );
                }
            }

            if (purgeRegistryKey)
            {
                Logger.Singleton.LifecycleEvent("Deleting registry sub key 'HKLM:{0}'", versioningRegSubKey);
                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(versioningRegSubKey, false);
                    Logger.Singleton.LifecycleEvent("Successfully registry sub key 'HKLM:{0}'!", versioningRegSubKey);
                }
                catch (Exception ex)
                {
                    Logger.Singleton.Warning("Could not delete registry sub key 'HKLM:{0}' because '{1}'", versioningRegSubKey, ex.Message);
                }
            }

            Logger.Singleton.Info("Purge finished!");

            return;
        }

        TryCreateEventLog(typeof(Program).Namespace, typeof(Program).Namespace);

        var app = new ServiceHostApp();
        app.EventLog.Source = typeof(Program).Namespace;
        app.EventLog.Log = typeof(Program).Namespace;

        Console.CancelKeyPress += (_, _) => app.Stop();

        app.HostOpening += AutoDeployerService.Start;
        app.HostClosing += AutoDeployerService.Stop;
        app.Process(args.ToArray());
    }

    private static void TryCreateEventLog(string source, string log)
    {
#if NETFRAMEWORK
        try
        {
            if (EventLog.Exists(log) && EventLog.SourceExists(source)) return;

            EventLog.CreateEventSource(source, log);

            var eventLog = new EventLog(source, ".", log);

            if (eventLog.OverflowAction != OverflowAction.OverwriteAsNeeded)
            {
                eventLog.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, 5);
                eventLog.MaximumKilobytes = 16000;
            }
        }
        catch { }
#endif
    }
}