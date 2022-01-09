﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MFDLabs.Configuration.Logging;
using MFDLabs.Configuration.Settings;
using MFDLabs.Hashicorp.VaultClient;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AppRole;
using MFDLabs.Hashicorp.VaultClient.V1.Commons;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines.KeyValue.V2;

namespace MFDLabs.Configuration.Clients.Vault
{
    internal static class KeyValueSecretsEngineV2Extensions
    {
        public static bool PathExists(this IKeyValueSecretsEngineV2 kv, string path, out Secret<ListInfo> o, string mountPoint = null)
        {
            o = null;
            
            try
            {
                o = kv.ReadSecretPathsAsync(path, mountPoint).Result;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    
    public class VaultConfigurationClient
    {
        private const string BaseMountPoint = "mfdlabs-sharp-v2/";

#if DEBUG
        private const string AppConfiguration = "Debug";
#else
        private const string AppConfiguration = "Release";
#endif

        public VaultConfigurationClient(string address, string roleId, string secretId)
        {
            var authMethod = new AppRoleAuthMethodInfo(roleId, secretId);
            var settings = new VaultClientSettings(address, authMethod);
            _client = new VaultClient(settings);
            _vaultClientRefreshTimer = new Timer(RefreshToken, null, TimeSpan.FromHours(0.75), TimeSpan.FromHours(0.75));
        }

        private void RefreshToken(object s)
        {
            _vaultClientRefreshTimer.Change(-1, -1);
            ConfigurationLogging.Info("Renewing vault client's token, '{0}...'",
                _client.V1.Auth.Token.LookupSelfAsync()
                    .Result.Data.Id.Substring(0, 6));
            _client.V1.Auth.Token.RenewSelfAsync().Wait();
            _vaultClientRefreshTimer.Change(TimeSpan.FromHours(0.75), TimeSpan.FromHours(0.75));
        }

        public static IReadOnlyCollection<T> FetchWithRetries<T>(Func<string, IReadOnlyCollection<T>> getterFunc, string groupName, int maxAttempts)
        {
            for (var i = 0; i < maxAttempts; i++)
            {
                try
                {
                    return getterFunc(groupName);
                }
                catch (Exception ex)
                {
                    ConfigurationLogging.Error(ex.ToString());
                    Thread.Sleep(i * global::MFDLabs.Configuration.Properties.Settings.Default.VaultConfigurationFetcherBackoffBaseMilliseconds);
                }
            }
            return Array.Empty<T>();
        }

        public IReadOnlyCollection<ISetting> GetAllSettings(string groupName)
        {
            if (!_client.V1.Secrets.KeyValue.V2.PathExists($"{AppConfiguration}/{groupName}", out var paths,
                    BaseMountPoint))
                return Array.Empty<ISetting>();
            
            return (from values in
                    from secret in
                        from path in
                            paths.Data.Keys
                        select _client.V1.Secrets.KeyValue.V2.ReadSecretAsync($"{AppConfiguration}/{groupName}/{path}",
                            mountPoint: BaseMountPoint).Result
                    select secret.Data.Data
                select new Setting
                {
                    GroupName = groupName,
                    Name = (string) values["Name"],
                    Type = (string) values["Type"],
                    Updated = (DateTime) values["Updated"],
                    Value = (string) values["Value"]
                }).Cast<ISetting>().ToArray();
        }

        public IReadOnlyCollection<IConnectionString> GetAllConnectionStrings(string groupName)
        {
            if (!_client.V1.Secrets.KeyValue.V2.PathExists($"{AppConfiguration}/{groupName}", out var paths,
                    BaseMountPoint))
                return Array.Empty<IConnectionString>();
            
            return (from values in
                    from secret in
                        from path in
                            paths.Data.Keys
                        select _client.V1.Secrets.KeyValue.V2.ReadSecretAsync($"{AppConfiguration}/{groupName}/{path}",
                            mountPoint: BaseMountPoint).Result
                    select secret.Data.Data
                select new ConnectionString
                {
                    GroupName = groupName,
                    Name = (string) values["Name"],
                    Updated = (DateTime) values["Updated"],
                    Value = (string) values["Value"]
                }).Cast<IConnectionString>().ToArray();
        }

        public void SetProperty(string groupName, string name, string type, string value, DateTime updated)
        {
            var values = new Dictionary<string, object>
            {
                { "Name", name },
                { "Type", type },
                { "Value", value },
                { "Updated", updated }
            };
            _client.V1.Secrets.KeyValue.V2.WriteSecretAsync($"{AppConfiguration}/{groupName}/{name}", values, mountPoint: BaseMountPoint);
        }

        private readonly IVaultClient _client;
        private readonly Timer _vaultClientRefreshTimer;
    }
}
