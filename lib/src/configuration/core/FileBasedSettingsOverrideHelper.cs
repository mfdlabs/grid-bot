namespace Configuration;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

/// <summary>
/// Helper for file based overrides.
/// </summary>
public static class FileBasedSettingsOverrideHelper
{
    /// <summary>
    /// Read the overriden settnings from the <paramref name="fileName"/>
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="errorLogger">Optional method for logging errors.</param>
    /// <returns>The overridden settings or null.</returns>
    public static Dictionary<string, object> ReadOverriddenSettingsFromFilePath(string fileName, Action<string, object[]> errorLogger = null)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        string contents;
        try
        {
            using var stream = File.Open(fileName, FileMode.Open);
            using var reader = new StreamReader(stream);

            contents = reader.ReadToEnd();
        }
        catch (Exception ex)
        {
            errorLogger?.Invoke("Unable to read file :{0} Exception: {1}", new[] { fileName, ex.ToString() });

            return null;
        }

        return ReadOverriddenSettingsFromFileContent(contents, errorLogger);
    }

    private static Dictionary<string, object> ReadOverriddenSettingsFromFileContent(string fileContent, Action<string, object[]> errorLogger = null, Action<string, object[]> informationLogger = null)
    {
        if (string.IsNullOrWhiteSpace(fileContent)) return null;

        var overriddenSettings = new Dictionary<string, object>();
        try
        {
            foreach (var element in XDocument.Parse(fileContent).Descendants("setting"))
            {
                if (element.Parent?.Name == null) continue;

                var key = $"{element.Parent.Name}.{element.Attribute("name")?.Value}";

                var value = element.Attribute("serializeAs")?.Value;
                if (string.IsNullOrEmpty(value))
                {
                    errorLogger?.Invoke("File based override setting:{0} 'serializeAs' attribute not provided. Skipping", new[] { key });

                    continue;
                }

                if (string.IsNullOrEmpty(element.Descendants("value").First().Value))
                {
                    errorLogger?.Invoke("File based override does not contain a value for setting name:{0}", new[] { key });

                    continue;
                }

                var valueType = Type.GetType(value);
                if (valueType == null)
                {
                    errorLogger?.Invoke("File based override setting:{0} has Invalid type (serializeAs) :{1}", new[] { key, value });

                    continue;
                }

                var newValue = Convert.ChangeType(element.Descendants("value").First().Value, valueType);

                informationLogger?.Invoke("Reading file based setting-Name:{0} with value:{1}", new[] { key, newValue });

                overriddenSettings[key] = newValue;
            }
        }
        catch (Exception ex)
        {
            errorLogger?.Invoke($"There was an exception while reading file-based override settings. FileContents:{0} Exception:{1}", new[] { fileContent, ex.ToString() });
            return null;
        }

        return overriddenSettings;
    }
}
