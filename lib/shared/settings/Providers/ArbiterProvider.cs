namespace Grid.Bot;

using System;

using IArbiterSettings = Grid.ISettings;

/// <summary>
/// Settings provider for all arbiter related stuff.
/// </summary>
public class ArbiterSettings : BaseSettingsProvider<ArbiterSettings>, IArbiterSettings
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.ArbiterPath;

    /// <summary>
    /// Gets the default grid server timeout.
    /// </summary>
    public TimeSpan GridServerArbiterDefaultTimeout => GetOrDefault(
        nameof(GridServerArbiterDefaultTimeout),
        TimeSpan.FromMinutes(1)
    );

    /// <summary>
    /// Gets the timeout for the script execution grid-server arbiter.
    /// </summary>
    public TimeSpan ScriptExecutionArbiterMaxTimeout => GetOrDefault(
        nameof(ScriptExecutionArbiterMaxTimeout),
        TimeSpan.FromSeconds(15)
    );

    /// <inheritdoc cref="IArbiterSettings.DefaultLeasedGridServerInstanceLease"/>
    public TimeSpan DefaultLeasedGridServerInstanceLease => GetOrDefault(
        nameof(DefaultLeasedGridServerInstanceLease),
        TimeSpan.FromMinutes(5)
    );

    /// <inheritdoc cref="IArbiterSettings.GridServerExecutableName"/>
    public string GridServerExecutableName => GetOrDefault(
        nameof(GridServerExecutableName),
        "gridserver.exe"
    );

    /// <inheritdoc cref="IArbiterSettings.GridServerRegistryKeyName"/>
    public string GridServerRegistryKeyName => GetOrDefault(
        nameof(GridServerRegistryKeyName),
        @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\ROBLOX Corporation\Roblox"
    );

    /// <inheritdoc cref="IArbiterSettings.GridServerRegistryValueName"/>
    public string GridServerRegistryValueName => GetOrDefault(
        nameof(GridServerRegistryValueName),
        "RccServicePath"
    );
}
