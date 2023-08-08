using System;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.Threading.Tasks;
using System.CommandLine.Parsing;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using Microsoft.Win32;
using MFDLabs.Logging;
using MFDLabs.FileSystem;
using MFDLabs.Diagnostics;
using MFDLabs.Text.Extensions;
using MFDLabs.Configuration.Logging;
using MFDLabs.Grid.AutoDeployer.Service;

namespace MFDLabs.Grid.AutoDeployer;

internal static class Program
{
    private static ILogger _logger;
    private static readonly Option<bool> PurgeOption = new(new[] { "-purge", "/purge", "--purge" }, "Purge all known deployer info.") { IsRequired = false };

    public static async Task Main(params string[] args)
    {
        _logger = new Logger(
            name: "grid-auto-deployer",
            logLevel: global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.EnvironmentLogLevel,
            logWithColor: global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.EnvironmentLoggerUseColor
        );

        ConfigurationLogging.OverrideDefaultConfigurationLogging(
            _logger.Error,
            _logger.Warning,
            _logger.Info
        );

        // If args has -purge or --purge etc.
        // purge all known deployer info.
        var rootCommand = new RootCommand(
            description: "Polls Github Cloud or Github Enterprise constantly for new releases to deploy to the host machine."
        )
        {
            PurgeOption,
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
            _logger.Warning("--purge set. Purging deployment files...");

            var deploymentPath = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.DeploymentPath;
            var versioningRegSubKey = global::MFDLabs.Grid.AutoDeployer.Properties.Settings.Default.VersioningRegistrySubKey;

            // If null, just exit and warn.
            if (deploymentPath.IsNullOrEmpty())
            {
                _logger.Warning("Deployment path not set, cannot clear deployment directory. Exiting...");
                return;
            }
            if (versioningRegSubKey.IsNullOrEmpty())
            {
                _logger.Warning("Version Registry SubKey not set, cannot delete registry key. Exiting...");
                return;
            }

            var purgeDeploymentPath = true;
            var purgeRegistryKey = true;

            if (!Directory.Exists(deploymentPath))
            {
                _logger.Warning("Deployment path at '{0}' does not exist or is a file. Ignoring...", deploymentPath);
                purgeDeploymentPath = false;
            }

            if (Registry.LocalMachine.OpenSubKey(versioningRegSubKey) == null)
            {
                _logger.Warning("Version Registry SubKey at 'HKLM:{0}' did not exist. Ignoring...", versioningRegSubKey);
                purgeRegistryKey = false;
            }

            if (purgeDeploymentPath)
            {
                foreach (var directory in Directory.EnumerateDirectories(deploymentPath)) // TODO: Match DeploymentId regex?
                {
                    _logger.LifecycleEvent("Deleting directory '{0}'...", directory);

                    directory.PollDeletionBlocking(
                        maxAttempts: 2,
                        onFailure: ex =>
                        {
                            if (ex is UnauthorizedAccessException)
                                _logger.Warning(
                                    "Could not delete directory because we do not have write access. " +
                                    "Please run this app with elevated permissions or allow the user '{0}\\{1}' " +
                                    "to write to the directory '{2}' and it's sub-directories. Ignoring...",
                                    SystemGlobal.GetMachineId(),
                                    ProcessHelper.GetCurrentUser(),
                                    directory
                                );
                            else
                                _logger.Warning("Could not delete directory '{0}' because '{1}'", directory, ex.Message);
                        },
                        onSuccess: () => _logger.LifecycleEvent("Successfully deleted directory '{0}'!", directory)
                    );
                }
            }

            if (purgeRegistryKey)
            {
                _logger.LifecycleEvent("Deleting registry sub key 'HKLM:{0}'", versioningRegSubKey);
                try
                {
                    Registry.LocalMachine.DeleteSubKeyTree(versioningRegSubKey, false);
                    _logger.LifecycleEvent("Successfully registry sub key 'HKLM:{0}'!", versioningRegSubKey);
                }
                catch (Exception ex)
                {
                    _logger.Warning("Could not delete registry sub key 'HKLM:{0}' because '{1}'", versioningRegSubKey, ex.Message);
                }
            }

            _logger.Info("Purge finished!");

            return;
        }

        Console.CancelKeyPress += (_, _) => AutoDeployerService.Stop();

        AutoDeployerService.Start(_logger);
    }
}
