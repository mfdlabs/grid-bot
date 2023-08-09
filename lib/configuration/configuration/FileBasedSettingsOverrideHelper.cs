using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Configuration
{
    public static class FileBasedSettingsOverrideHelper
    {
        public static Dictionary<string, object> ReadOverriddenSettingsFromFilePath(string fileName, Action<string, object[]> errorLogger = null)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            
            string contents;
            FileStream stream = null;
            StreamReader reader = null;
            try
            {
                stream = File.Open(fileName, FileMode.Open);
                reader = new StreamReader(stream);
                contents = reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                errorLogger?.Invoke($"Unable to read file :{fileName} Exception: {ex}", Array.Empty<object>());
                return null;
            }
            finally
            {
                reader?.Dispose();
                stream?.Dispose();
            }
            return ReadOverriddenSettingsFromFileContent(contents, errorLogger);
        }

        private static Dictionary<string, object> ReadOverriddenSettingsFromFileContent(string fileContent, Action<string, object[]> errorLogger = null, Action<string, object[]> informationLogger = null)
        {
            if (string.IsNullOrWhiteSpace(fileContent))
            {
                return null;
            }
            var overriddenSettings = new Dictionary<string, object>();
            try
            {
                foreach (var element in XDocument.Parse(fileContent).Descendants("setting"))
                {
                    if (element.Parent?.Name == null) continue;
                    
                    var key = $"{element.Parent.Name}.{element.Attribute("name")?.Value}";
                    var value = element.Attribute("serializeAs")?.Value;
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (!string.IsNullOrEmpty(element.Descendants("value").First().Value))
                        {
                            var valueType = Type.GetType(value);
                            if (valueType != null)
                            {
                                var newValue = Convert.ChangeType(element.Descendants("value").First().Value, valueType);
                                informationLogger?.Invoke($"Reading file based setting-Name:{key} with value:{newValue}", Array.Empty<object>());
                                overriddenSettings[key] = newValue;
                            }
                            else
                            {
                                errorLogger?.Invoke($"File based override setting:{key} has Invalid type (serializeAs) :{value}", Array.Empty<object>());
                            }
                        }
                        else
                        {
                            errorLogger?.Invoke($"File based override does not contain a value for setting name:{key}", Array.Empty<object>());
                        }
                    }
                    else
                    {
                        errorLogger?.Invoke($"File based override setting:{key} 'serializeAs' attribute not provided. Skipping", Array.Empty<object>());
                    }
                }
            }
            catch (Exception ex)
            {
                errorLogger?.Invoke($"There was an exception while reading file-based override settings. FileContents:{fileContent} Exception:{ex}", Array.Empty<object>());
                return null;
            }
            return overriddenSettings;
        }
    }
}
