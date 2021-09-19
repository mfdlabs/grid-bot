using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace MFDLabs.Configuration
{
    public class FileBasedSettingsOverrideHelper
    {
        public static Dictionary<string, object> ReadOverriddenSettingsFromFilePath(string fileName, Action<string, object[]> errorLogger = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return null;
            }
            string contents = null;
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
                errorLogger?.Invoke(string.Format("Unable to read file :{0} Exception: {1}", fileName, ex), new object[0]);
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                if (stream != null)
                {
                    stream.Dispose();
                }
            }
            return ReadOverriddenSettingsFromFileContent(contents, errorLogger, null);
        }

        public static Dictionary<string, object> ReadOverriddenSettingsFromFileContent(string fileContent, Action<string, object[]> errorLogger = null, Action<string, object[]> informationLogger = null)
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
                    if ((element.Parent?.Name) != null)
                    {
                        var key = string.Format("{0}.{1}", element.Parent.Name, element.Attribute("name")?.Value);
                        var value = element.Attribute("serializeAs")?.Value;
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (!string.IsNullOrEmpty(element.Descendants("value").First().Value))
                            {
                                var valueType = Type.GetType(value);
                                if (valueType != null)
                                {
                                    var newValue = Convert.ChangeType(element.Descendants("value").First().Value, valueType);
                                    informationLogger?.Invoke(string.Format("Reading file based setting-Name:{0} with value:{1}", key, newValue), new object[0]);
                                    overriddenSettings[key] = newValue;
                                }
                                else
                                {
                                    errorLogger?.Invoke(string.Format("File based override setting:{0} has Invalid type (serializeAs) :{1}", key, value), new object[0]);
                                }
                            }
                            else
                            {
                                errorLogger?.Invoke(string.Format("File based override does not contain a value for setting name:{0}", key), new object[0]);
                            }
                        }
                        else
                        {
                            errorLogger?.Invoke(string.Format("File based override setting:{0} 'serializeAs' attribute not provided. Skipping", key), new object[0]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errorLogger?.Invoke(string.Format("There was an exception while reading file-based override settings. FileContents:{0} Exception:{1}", fileContent, ex), new object[0]);
                return null;
            }
            return overriddenSettings;
        }
    }
}
