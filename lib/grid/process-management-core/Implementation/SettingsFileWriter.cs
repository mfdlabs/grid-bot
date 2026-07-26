namespace Grid.ProcessManagement.Core;

using System;
using System.IO;
using System.Text;
using System.Threading;

using Newtonsoft.Json;

using Logging;
using ClientSettings.Client;

/// <inheritdoc cref="ISettingsFileWriter"/>
public class SettingsFileWriter : ISettingsFileWriter
{

    private const int _MaxAttempts = 3;
    private const int _SleepIntervalMilliseconds = 50;

    private readonly ILogger _Logger;

    /// <summary>
    /// Constructs a new instance of <see cref="SettingsFileWriter"/>
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/></param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be null.</exception>
    public SettingsFileWriter(ILogger logger)
    {
        _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc cref="ISettingsFileWriter.WriteSettingsFile(string, ClientApplicationSettingsResponse)"/>
    public bool WriteSettingsFile(string filePath, ClientApplicationSettingsResponse rccApplicationSettings)
    {
        var fileContents = JsonConvert.SerializeObject(rccApplicationSettings);

        for (int i = 0; i < _MaxAttempts; i++)
        {
            try
            {
                _Logger.Information("WriteSettingsFile. Attempting to write settings file to {0}. Attempt #{1}", filePath, i + 1);
                _Logger.Verbose("WriteSettingsFile. FileContents: {0}", fileContents);

                TryWriteSettingsFile(filePath, fileContents, filePath + ".tmp");

                return true;
            }
            catch (Exception ex)
            {
                _Logger.Error("WriteSettingsFile. Error: {0}", ex);
            }

            Thread.Sleep(_SleepIntervalMilliseconds);
        }

        return false;
    }

    private void TryWriteSettingsFile(string filePath, string fileContents, string tempFilePath)
    {
        if (File.Exists(filePath))
        {
            _Logger.Debug("TryWriteSettingsFile. {0} already exists. Attempting to write fileContents to {1}", filePath, tempFilePath);
            File.WriteAllText(tempFilePath, fileContents, Encoding.ASCII);

            _Logger.Debug("TryWriteSettingsFile. Attempting to replace {0} with {1}", filePath, tempFilePath);
            File.Replace(tempFilePath, filePath, null, true);

            return;
        }

        _Logger.Debug("TryWriteSettingsFile. {0} does not exist. Attempting to write fileContents to {1}", filePath, filePath);

        File.WriteAllText(filePath, fileContents, Encoding.ASCII);
    }
}
