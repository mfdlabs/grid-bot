namespace Grid.AutoDeployer;

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
            refreshInterval: SettingsProvidersDefaults.DefaultRefreshInterval
#endif
        )
    {
    }
}

/// <summary>
/// Base provider class for grid operations.
/// </summary>
/// <typeparam name="TProvider">The type of the singleton.</typeparam>
public abstract class BaseSettingsProvider<TProvider> : BaseSettingsProvider
    where TProvider : class, new()
{
    /// <summary>
    /// Exposes a singleton of this provider.
    /// </summary>
    public static readonly TProvider Singleton = new();
}
