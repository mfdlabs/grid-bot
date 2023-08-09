using System;
using System.Linq;

using Discord.WebSocket;

using Logging;

using Reflection.Extensions;

namespace Discord.Configuration
{
    public static class DiscordConfigurationHelper
    {
        public static void InitializeClient(string address, string token)
        {
            CheckClientAlreadyExists();

            _client = new VaultConfigurationClient(address, token);
        }
        
        public static void InitializeClient(string address, string roleId, string secretId)
        {
            CheckClientAlreadyExists();

            _client = new VaultConfigurationClient(address, roleId, secretId);
        }
        
        public static void InitializeClient(string address, (string, string) ldapCreds)
        {
            CheckClientAlreadyExists();

            _client = new VaultConfigurationClient(address, ldapCreds);
        }

        private static void CheckClientAlreadyExists()
        {
            if (_client != null)
                throw new InvalidOperationException(
                    "The DiscordConfigurationHelper has already been initialized!");
        }

        private static void CheckUnitialized()
        {
            if (_client == null)
                throw new InvalidOperationException("The DiscordConfigurationHelper hasn't been initialized!");
        }

        public static void WriteMetaValue<T>(this SocketMessage message, string groupName, string settingName, T value)
        {
            CheckUnitialized();
            
            message.CheckCanWrite(groupName);
            
            _client.WriteMetaValue(message, groupName, settingName, value);
        }
        
        public static void WriteSetting<T>(this SocketMessage message, string groupName, string settingName, T value)
        {
            CheckUnitialized();
            
            message.CheckCanWrite(groupName);
            
            _client.WriteSetting(message, groupName, settingName, value);
        }

        public static T GetSetting<T>(
            this SocketMessage message,
            string groupName,
            string settingName,
            bool requireAuthorization = false
        )
        {
            CheckUnitialized();

            message.CheckCanRead(groupName, requireAuthorization);

            var rawSettingValue = _client.GetSettingValue(message, groupName, settingName);
            
            
            var type = typeof(T);
            object transformedValue;

            try
            {
                if (type.IsEnum)
                {
                    transformedValue = Enum.Parse(type, (string)rawSettingValue);
                }
                else if (type.IsPrimitive())
                {
                    transformedValue = Convert.ChangeType(rawSettingValue, type);
                }
                else if (type == typeof(TimeSpan))
                {
                    // Specific here because the only class we actually use in settings is
                    // TimeSpan, everything else is either a primitive or Enum
                    transformedValue = TimeSpan.Parse((string)rawSettingValue);
                }
                else
                {
                    transformedValue = rawSettingValue;
                }
            }
            catch (Exception ex)
            {
                Logger.Singleton.Warning(ex.Message);

                switch (ex)
                {
                    case ArgumentNullException or ArgumentException:
                        Logger.Singleton.Warning("There was an argument exception with your setting value " +
                                                       $"when trying to cast it to '{type.FullName}', {ex.Message}.");
                        return default;
                    case InvalidCastException:
                    case FormatException:
                    case OverflowException:
                        Logger.Singleton.Warning("The typeof your setting value could not be casted to the " +
                                                       $"type of the real setting value, which is  '{type.FullName}', please try again.");
                        return default;
                    default:
                        Logger.Singleton.Warning($"An unknown exception occurred when trying to update the setting '{settingName}'.");

                        return default;
                }
            }

            return (T) transformedValue;
        }

        private static void CheckCanWrite(this SocketMessage message, string groupName)
        {
            var allowedWriters = ((string) _client.GetMetaValue(message, groupName, "AllowedWriterIDs")).Split(',');
            if (allowedWriters.Contains("*")) return;
            
            var ownerId = message.Author.Id;

            if (!allowedWriters.Contains(ownerId.ToString()))
                throw new AccessViolationException(
                    $"The user '{ownerId}' does not have permission to write to that setings configuration");
        }
        
        private static void CheckCanRead(this SocketMessage message, string groupName, bool requireAuthorization)
        {
            if (!requireAuthorization) return;
            
            var allowedReaders = ((string) _client.GetMetaValue(message, groupName, "AllowedReaderIDs")).Split(',');
            if (allowedReaders.Contains("*")) return;
            
            var ownerId = message.Author.Id;

            if (!allowedReaders.Contains(ownerId.ToString()))
                throw new AccessViolationException(
                    $"The user '{ownerId}' does not have permission to read that setings configuration");
        }

        private static VaultConfigurationClient _client;
    }
}
