namespace Grid.Bot;

using Configuration;

/// <summary>
/// Base provider class for grid operations.
/// </summary>
public abstract class BaseSettingsProvider
#if USE_VAULT_SETTINGS_PROVIDER
    : VaultProvider
#else
    : EnvironmentProvider
#endif
{
#if USE_VAULT_SETTINGS_PROVIDER
    /// <inheritdoc cref="IVaultProvider.Mount"/>
    public override string Mount => SettingsProvidersDefaults.MountPath;
#else
    /// <inheritdoc cref="IVaultProvider.Path"/>
    public abstract string Path { get; }
#endif

    /// <summary>
    /// Construct a new instance of <see cref="BaseSettingsProvider"/>
    /// </summary>
    protected BaseSettingsProvider()
        : base(
#if USE_VAULT_SETTINGS_PROVIDER
            refreshInterval: SettingsProvidersDefaults.DefaultRefreshInterval,
            logger: Logging.Logger.Singleton
#endif
        )
    {
    }
}
