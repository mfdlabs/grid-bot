namespace Grid.Bot;

/// <summary>
/// Options for all maintenance related settings.
/// </summary>
public class MaintenanceOptions
{
    /// <summary>
    /// The configuration section these options bind from.
    /// </summary>
    public const string SectionName = "Maintenance";

    /// <summary>
    /// The vault path for the maintenance options.
    /// </summary>
    public const string VaultPath = "maintenance";

    /// <summary>
    /// Gets or sets a value indicating whether maintenance is enabled.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public bool MaintenanceEnabled { get; set; }

    /// <summary>
    /// Gets or sets the maintenance status message.
    /// </summary>
    /// <remarks>Written at runtime through <see cref="ISettingsWriter"/>.</remarks>
    public string MaintenanceStatus { get; set; } = string.Empty;
}
