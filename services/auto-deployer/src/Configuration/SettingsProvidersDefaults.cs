namespace Grid.AutoDeployer;

using System;

internal static class SettingsProvidersDefaults
{
    private const string _vaultMountEnvVar = "VAULT_MOUNT";

#if DEBUG
    private const string ProviderPathConfiguration = "debug";
#else
    private const string ProviderPathConfiguration = "release";
#endif

    public const string ProviderPathPrefix = "grid-auto-deployer";

    public const string BacktracePath = $"{ProviderPathPrefix}/backtrace/{ProviderPathConfiguration}";
    public const string AutoDeployerPath = $"{ProviderPathPrefix}/auto-deployer/{ProviderPathConfiguration}";

    public const string DefaultMountPath = "grid-bot-settings";
    public static string MountPath = Environment.GetEnvironmentVariable(_vaultMountEnvVar) ?? DefaultMountPath;

    public static readonly TimeSpan DefaultRefreshInterval = TimeSpan.FromMinutes(45);
    public static readonly TimeSpan VaultClientTokenRefreshInterval = TimeSpan.FromHours(.75);
}
