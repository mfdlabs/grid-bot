using System;
using System.Linq;
using System.Threading;
using System.Configuration;
using System.Collections.Generic;

using Discord.WebSocket;

using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.LDAP;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

using Logging;

using MFDLabs.Threading.Extensions;

namespace MFDLabs.Discord.Configuration
{
    internal static class IKeyValueSecretsEngineV2Extensions
    {
        public static bool SecretExists<T>(this IKeyValueSecretsEngineV2 kv, string path,  out T value, string mountPoint = null)
        {
            value = default;
            try
            {
                value = kv.ReadSecretAsync<T>(path, null, mountPoint).Sync().Data.Data;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    internal class VaultConfigurationClient
    {
        private const string SecretsEngineBasePath = "client-products/";
        private const string MetaValuePrefix = "__META__";
        public const string AllowedWriters = $"{MetaValuePrefix}AllowedWriterIDs";
        public const string AllowedReaders = $"{MetaValuePrefix}AllowedReaderIDs";
        
#if DEBUG
        private const string AppConfiguration = "Debug";
#else
        private const string AppConfiguration = "Release";
#endif

        public VaultConfigurationClient(string address, string token) 
            => InitializeFields(address, new TokenAuthMethodInfo(token));

        public VaultConfigurationClient(string address, string roleId, string secretId)
            => InitializeFields(address, new AppRoleAuthMethodInfo(roleId, secretId));

        public VaultConfigurationClient(string address, (string, string) ldapUserNamePassword)
            => InitializeFields(address, new LDAPAuthMethodInfo(ldapUserNamePassword.Item1, ldapUserNamePassword.Item2));
        
        private void InitializeFields(string address, IAuthMethodInfo authMethod)
        {
            var settings = new VaultClientSettings(address, authMethod);
            var client = new VaultClient(settings);
            _kvV2 = client.V1.Secrets.KeyValue.V2;
            _token = client.V1.Auth.Token;
            _vaultClientRefreshTimer = new(RefreshToken, null, TimeSpan.FromHours(0.75), TimeSpan.FromHours(0.75));
            new Timer(RefreshCache, null, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(15));
        }

        // This is only here in case we modify it directly on Vault
        private void RefreshCache(object s)  => _cachedSettings.Clear();
        
        private void RefreshToken(object s)
        {
            _tokenStr ??= _token.LookupSelfAsync().Sync().Data.Id.Substring(0, 6);

            _vaultClientRefreshTimer.Change(-1, -1);
            Logger.Singleton.Information("Renewing vault client's token, '{0}...'", _tokenStr);
            _token.RenewSelfAsync().Wait();
            _vaultClientRefreshTimer.Change(TimeSpan.FromHours(0.75), TimeSpan.FromHours(0.75));
        }
        
        
        private static IDictionary<string, object> FetchWithRetries(Func<IDictionary<string, object>> getterFunc, int maxAttempts)
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    return getterFunc();
                }
                catch (Exception ex)
                {
#if DEBUG || DEBUG_LOGGING_IN_PROD
                    Logger.Singleton.Error(ex);
#else
                    Logger.Singleton.Warning(ex.Message);
#endif
                    Thread.Sleep(i * 100);
                }
            }

            return default;
        }

        private void SetupDefaultConfiguration(SocketMessage message, string groupName)
        {
            var defaultConfigPath = GetDefaultConfigurationPath(groupName);
            
            var guildId = message.Channel is SocketGuildChannel channel ? channel.Guild.Id : message.Channel.Id;

            
            if (!_kvV2.SecretExists<Dictionary<string, string>>(defaultConfigPath, out var data, SecretsEngineBasePath))
                throw new InvalidOperationException($"Unable to setup default configuration for '{groupName}' " +
                                                    $"for guild '{guildId}': There was no default configuration at the " +
                                                    $"path '{SecretsEngineBasePath}{defaultConfigPath}'");

            var ownerId = message.Channel is SocketGuildChannel c ? c.Guild.OwnerId : message.Author.Id;

            var allowedReaders = data[AllowedReaders];
            var allowedWriters = data[AllowedWriters];

            if (!allowedReaders.Contains(ownerId.ToString()))
                allowedReaders = $"{allowedReaders},{ownerId}";
            if (!allowedWriters.Contains(ownerId.ToString()))
                allowedWriters = $"{allowedWriters},{ownerId}";


            data.Remove(AllowedWriters);
            data.Remove(AllowedReaders);
            
            data.Add(AllowedWriters, allowedWriters);
            data.Add(AllowedReaders, allowedReaders);
            
            _kvV2.WriteSecretAsync(GetSharedPath(message, groupName), data, mountPoint: SecretsEngineBasePath).Wait();
        }

        private bool HasCachedSettings(ulong guildId, string groupName)
            => HasCachedSettings(guildId, groupName, out _);
        
        private bool HasCachedSettings(ulong guildId, string groupName, out IDictionary<string, object> settings)
        {
            settings = (from s in _cachedSettings where s.Item1 == (guildId, groupName) select s.Item2)
                .FirstOrDefault();

            return settings != default;
        }

        private void WriteToCache(SocketMessage message, string groupName, IDictionary<string, object> settings)
        {
            var guildId = message.Channel is SocketGuildChannel channel ? channel.Guild.Id : message.Channel.Id;
            
            if (HasCachedSettings(guildId, groupName)) 
                _cachedSettings.RemoveAll(s => s.Item1 == (guildId, groupName));
            
            _cachedSettings.Add(((guildId, groupName), settings));
        }

        private IDictionary<string, object> GetSettingsWithMetaValuesNoCache(SocketMessage message, string groupName)
        {
            var path = GetSharedPath(message, groupName);
            
            if (!_kvV2.SecretExists<IDictionary<string, object>>(path, out var d, SecretsEngineBasePath))
                SetupDefaultConfiguration(message, groupName);

            d ??= _kvV2.ReadSecretAsync<IDictionary<string, object>>(
				path, 
				mountPoint: SecretsEngineBasePath
			).Sync().Data.Data;

            return d;
        }

        private IDictionary<string, object> GetSettingsInternal(SocketMessage message, string groupName)
        {
            var guildId = message.Channel is SocketGuildChannel channel ? channel.Guild.Id : message.Channel.Id;
            
            if (HasCachedSettings(guildId, groupName, out var d))
                return d;
            
            var path = GetSharedPath(message, groupName);
            
            if (!_kvV2.SecretExists(path, out d, SecretsEngineBasePath))
                SetupDefaultConfiguration(message, groupName);

            d ??= _kvV2.ReadSecretAsync<IDictionary<string, object>>(
				path, 
				mountPoint: SecretsEngineBasePath
			).Sync().Data.Data;

            d = d.Where(k => !k.Key.StartsWith(MetaValuePrefix)).ToDictionary(k => k.Key, v => v.Value);

            _cachedSettings.Add(((guildId, groupName), d));
            
            return d;
        }

        public IDictionary<string, object> GetSettings(SocketMessage message, string groupName)
            => FetchWithRetries(() => GetSettingsInternal(message, groupName), 5);

        private static object GetSettingValueInternal(IDictionary<string, object> settings, string settingName)
            => (from s in settings where s.Key == settingName select s.Value).FirstOrDefault();

        public object GetSettingValue(SocketMessage message, string groupName, string settingName)
            => GetSettingValueInternal(GetSettings(message, groupName), settingName);

        public static bool SettingExistsInternal(IDictionary<string, object> settings, string settingName)
            => GetSettingValueInternal(settings, settingName) != default;
        
        public bool SettingExists(SocketMessage message, string groupName, string settingName)
            => GetSettingValue(message, groupName, settingName) != default;

        public object GetOrWriteMetaValue(SocketMessage message, string groupName, string settingName, object value)
        {
            if (GetMetaValue(message, groupName, settingName) == default)
                WriteMetaValue(message, groupName, settingName, value);

            return GetMetaValue(message, groupName, settingName);
        }

        public object GetMetaValue(SocketMessage message, string groupName, string settingName)
        {
            var settings = GetSettingsWithMetaValuesNoCache(message, groupName);
            return (from s in settings where s.Key == MetaValuePrefix + settingName select s.Value).FirstOrDefault();
        }
        
        public void WriteMetaValue(SocketMessage message, string groupName, string settingName, object value)
        {
            var settings = GetSettingsWithMetaValuesNoCache(message, groupName);

            var key = $"{MetaValuePrefix}{settingName}";

            if (!SettingExistsInternal(settings, key))
                settings.Add(key, value);
            else
                settings[key] = value;
            
            var path = GetSharedPath(message, groupName);

            _kvV2.WriteSecretAsync(path, settings, mountPoint: SecretsEngineBasePath).Wait();
        }
        
        public void WriteSetting(SocketMessage message, string groupName, string settingName, object value)
        {
            if (settingName.StartsWith(MetaValuePrefix))
                throw new InvalidOperationException("Unable to write a metavalue, please use WriteMetaValue()");
            
            var settings = GetSettingsWithMetaValuesNoCache(message, groupName);

            if (!SettingExistsInternal(settings, settingName))
                throw new SettingsPropertyNotFoundException($"Unknown guild scoped setting '{settingName}'");

            settings[settingName] = value;
            
            WriteToCache(message, groupName, settings);
            
            var path = GetSharedPath(message, groupName);

            // no wait here because it's already in cache.
            _kvV2.WriteSecretAsync(path, settings, mountPoint: SecretsEngineBasePath);
        }

        private static string GetSharedPath(SocketMessage message, string groupName)
        {
            var guildId = message.Channel is SocketGuildChannel channel ? channel.Guild.Id : message.Channel.Id;
            
            
            return $"{GetBasePath(groupName)}/{guildId}";
        }

        private static string GetDefaultConfigurationPath(string groupName)
            => $"{GetBasePath(groupName)}/default";
        
        private static string GetBasePath(string groupName) =>
            $"discord-guild-settings/{AppConfiguration}/{groupName}";

        private string _tokenStr;
        private Timer _vaultClientRefreshTimer;
        private IKeyValueSecretsEngineV2 _kvV2;
        private ITokenAuthMethod _token;

        private readonly List<((ulong, string), IDictionary<string, object>)> _cachedSettings = new();
    }
}
