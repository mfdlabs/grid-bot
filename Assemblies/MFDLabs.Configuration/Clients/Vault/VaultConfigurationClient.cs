using System;
using System.Collections.Generic;
using System.Threading;
using MFDLabs.Configuration.Logging;
using MFDLabs.Configuration.Settings;
using MFDLabs.Hashicorp.VaultClient;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.AppRole;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines;

namespace MFDLabs.Configuration.Clients.Vault
{
    public class VaultConfigurationClient
    {
#if DEBUG
        const string AppConfiguration = "Debug";
#else
        const string AppConfiguration = "Release";
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
            ConfigurationLogging.Info("Refreshing vault client's token, current is '{0}'", _client.V1.Auth.Token.LookupSelfAsync().Result.Data.Id);
            _client.V1.Auth.Token.RenewSelfAsync().Wait();
            ConfigurationLogging.Info("Refreshed vault client's token, new token is '{0}'", _client.V1.Auth.Token.LookupSelfAsync().Result.Data.Id);
            _vaultClientRefreshTimer.Change(TimeSpan.FromHours(0.75), TimeSpan.FromHours(0.75));
        }

        public IReadOnlyCollection<T> FetchWithRetries<T>(Func<string, IReadOnlyCollection<T>> getterFunc, string groupName, int maxAttempts)
        {
            for (int i = 0; i < maxAttempts; i++)
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
            return new T[0];
        }

        public IReadOnlyCollection<ISetting> GetAllSettings(string groupName)
        {
            var engine = new SecretsEngine()
            {
                Type = SecretsEngineType.KeyValueV2,
                Config = new Dictionary<string, object>
                    {
                        {  "version", "2" }
                    },
                Path = "mfdlabs-sharp-v2/"
            };

            var theseSettings = new List<ISetting>();

            var paths = _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync($"{AppConfiguration}/{groupName}", engine.Path).Result;
            foreach (var path in paths.Data.Keys)
            {
                var secret = _client.V1.Secrets.KeyValue.V2.ReadSecretAsync($"{AppConfiguration}/{groupName}/{path}", mountPoint: engine.Path).Result;
                var values = secret.Data.Data;
                theseSettings.Add(new Setting()
                {
                    GroupName = groupName,
                    Name = (string)values["Name"],
                    Type = (string)values["Type"],
                    Updated = (DateTime)values["Updated"],
                    Value = (string)values["Value"]
                });
            }
            return theseSettings.ToArray();
        }

        public IReadOnlyCollection<IConnectionString> GetAllConnectionStrings(string groupName)
        {
            var engine = new SecretsEngine()
            {
                Type = SecretsEngineType.KeyValueV2,
                Config = new Dictionary<string, object>
                    {
                        {  "version", "2" }
                    },
                Path = "mfdlabs-sharp-v2/"
            };

            var theseSettings = new List<IConnectionString>();

            var paths = _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync($"{AppConfiguration}/{groupName}", engine.Path).Result;
            foreach (var path in paths.Data.Keys)
            {
                var secret = _client.V1.Secrets.KeyValue.V2.ReadSecretAsync($"{AppConfiguration}/{groupName}/{path}", mountPoint: engine.Path).Result;
                var values = secret.Data.Data;
                theseSettings.Add(new ConnectionString()
                {
                    GroupName = groupName,
                    Name = (string)values["Name"],
                    Updated = (DateTime)values["Updated"],
                    Value = (string)values["Value"]
                });
            }
            return theseSettings.ToArray();
        }

        public void SetProperty(string groupName, string name, string type, string value, DateTime updated)
        {
            var engine = new SecretsEngine()
            {
                Type = SecretsEngineType.KeyValueV2,
                Config = new Dictionary<string, object>
                {
                    {  "version", "2" }
                },
                Path = "mfdlabs-sharp-v2/"
            };

            var values = new Dictionary<string, object>
            {
                { "Name", name },
                { "Type", type },
                { "Value", value },
                { "Updated", updated }
            };
            _client.V1.Secrets.KeyValue.V2.WriteSecretAsync($"{AppConfiguration}/{groupName}/{name}", values, mountPoint: engine.Path);
        }

        private readonly IVaultClient _client;
        private readonly Timer _vaultClientRefreshTimer;
    }
}
