using System;
using System.Collections.Generic;
using System.Threading;
using MFDLabs.Configuration.Logging;
using MFDLabs.Configuration.Settings;
using MFDLabs.Hashicorp.VaultClient;
using MFDLabs.Hashicorp.VaultClient.V1.AuthMethods.Token;
using MFDLabs.Hashicorp.VaultClient.V1.SecretsEngines;

namespace MFDLabs.Configuration.Clients.Vault
{
    public class VaultConfigurationClient
    {
        public VaultConfigurationClient(string address, string token)
        {
            var authMethod = new TokenAuthMethodInfo(token);
            var settings = new VaultClientSettings(address, authMethod);
            _client = new VaultClient(settings);
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
                Path = $"mfdlabs-sharp/{groupName}"
            };

            _client.V1.System.MountSecretBackendAsync(engine).Wait();
            var theseSettings = new List<ISetting>();

            var paths = _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync("", engine.Path).Result;
            foreach (var path in paths.Data.Keys)
            {
                var secret = _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: engine.Path).Result;
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
            _client.V1.System.UnmountSecretBackendAsync(engine.Path).Wait();
            return theseSettings;
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
                Path = $"mfdlabs-sharp/{groupName}"
            };

            _client.V1.System.MountSecretBackendAsync(engine).Wait();
            var theseSettings = new List<IConnectionString>();

            var paths = _client.V1.Secrets.KeyValue.V2.ReadSecretPathsAsync("", engine.Path).Result;
            foreach (var path in paths.Data.Keys)
            {
                var secret = _client.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: engine.Path).Result;
                var values = secret.Data.Data;
                theseSettings.Add(new ConnectionString()
                {
                    GroupName = groupName,
                    Name = (string)values["Name"],
                    Updated = (DateTime)values["Updated"],
                    Value = (string)values["Value"]
                });
            }
            _client.V1.System.UnmountSecretBackendAsync(engine.Path).Wait();
            return theseSettings;
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
                Path = $"mfdlabs-sharp/{groupName}"
            };

            _client.V1.System.MountSecretBackendAsync(engine).Wait();
            var values = new Dictionary<string, object>
            {
                { "Name", name },
                { "Type", type },
                { "Value", value },
                { "Updated", updated }
            };
            _client.V1.Secrets.KeyValue.V2.WriteSecretAsync(name, values, mountPoint: engine.Path).Wait();
            _client.V1.System.UnmountSecretBackendAsync(engine.Path).Wait();
        }

        private readonly IVaultClient _client;
    }
}
