namespace Grid.Bot;

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using Logging;

using Extensions;

/// <summary>
/// Syncs the Vault secrets with the current options structure from the local configuration.
/// </summary>
internal static class LocalConfigSync
{
    /// <summary>
    /// Adds settings missing from Vault (using the environment or default value) and removes settings
    /// that no longer exist, leaving existing Vault values untouched.
    /// </summary>
    /// <param name="dryRun">When true, only reports the changes.</param>
    public static async Task RunAsync(bool dryRun)
    {
        var logger = Logger.Singleton;

        logger.LogLevel = LogLevel.Verbose;
        logger.Information("Syncing local configuration to Vault{0} and exiting!", dryRun ? " (dry run)" : string.Empty);

        var services = new ServiceCollection();

        services.AddAppConfiguration(localOnly: true);

        using var provider = services.BuildServiceProvider();

        var global = provider.GetRequiredService<IOptions<GlobalOptions>>().Value;
        if (string.IsNullOrWhiteSpace(global.VaultAddress))
        {
            logger.Error("No Vault address is configured, nothing to sync to.");

            return;
        }

        var client = new VaultFactory(logger).CreateClient(global.VaultAddress, global.VaultCredential);
        var synchronizer = new VaultSettingsSynchronizer(client, EnvironmentDataProvider.VaultMountPath, logger);

        foreach (var section in SettingsSections.All)
        {
            var optionsType = typeof(IOptions<>).MakeGenericType(section.OptionsType);
            var options = optionsType.GetProperty(nameof(IOptions<object>.Value)).GetValue(provider.GetRequiredService(optionsType));

            var values = section.OptionsType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0)
                .ToDictionary(property => property.Name, property => SettingsValueFormatter.Format(property.GetValue(options)));

            await synchronizer.SyncAsync(GridBotVaultConfigurationExtensions.SectionPaths[section.Name], values, dryRun);
        }
    }
}
