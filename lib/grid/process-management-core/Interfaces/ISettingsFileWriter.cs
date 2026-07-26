namespace Grid.ProcessManagement.Core;

using ClientSettings.Client;

/// <summary>
/// Represents a class to write application settings.
/// </summary>
public interface ISettingsFileWriter
{
    /// <summary>
    /// Write the settings file.
    /// </summary>
    /// <param name="filePath">The path of the settings file.</param>
    /// <param name="rccApplicationSettings">The application settings response.</param>
    /// <returns>True if it successfully wrote.</returns>
    bool WriteSettingsFile(string filePath, ClientApplicationSettingsResponse rccApplicationSettings);
}
