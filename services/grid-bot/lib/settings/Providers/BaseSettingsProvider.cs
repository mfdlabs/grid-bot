namespace Grid.Bot;

using System.Collections.Generic;

using Configuration;

/// <summary>
/// Base provider class for grid operations.
/// </summary>
public abstract class BaseSettingsProvider : VaultProvider
{
    /// <inheritdoc cref="IVaultProvider.Mount"/>
    public override string Mount => SettingsProvidersDefaults.MountPath;

    /// <summary>
    /// Construct a new instance of <see cref="BaseSettingsProvider"/>
    /// </summary>
    protected BaseSettingsProvider()
        : base(Logging.Logger.Singleton)
    {
    }

    /// <summary>
    /// Gets the raw values associated with this settings provider.
    /// </summary>
    /// <returns>Te raw values.</returns>
    public IDictionary<string, object> GetRawValues() => _CachedValues;
}
