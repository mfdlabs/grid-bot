namespace Grid.Bot;

/// <summary>
/// Settings provider for all maintenance related stuff.
/// </summary>
public class MaintenanceSettings : BaseSettingsProvider
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.MaintenancePath;

    /// <inheritdoc cref="Configuration.IVaultProvider.AutomaticWrite"/>
    public override bool AutomaticWrite => false;

    /// <summary>
    /// Is maintenance enabled?
    /// </summary>
    public bool MaintenanceEnabled
    {
        get => GetOrDefault(nameof(MaintenanceEnabled), false);
        set => Set(nameof(MaintenanceEnabled), value);
    }

    /// <summary>
    /// Gets or sets the maintenance status message.
    /// </summary>
    public string MaintenanceStatus
    {
        get => GetOrDefault(nameof(MaintenanceStatus), string.Empty);
        set => Set(nameof(MaintenanceStatus), value);
    }
}
