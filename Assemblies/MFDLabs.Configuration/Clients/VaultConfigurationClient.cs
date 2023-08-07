using System;
using System.Linq;
using System.Threading;
using System.Text.Json;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.LDAP;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.SecretsEngines.KeyValue.V2;

using MFDLabs.Threading.Extensions;
using MFDLabs.Configuration.Logging;
using MFDLabs.Configuration.Settings;

namespace MFDLabs.Configuration.Clients.Vault
{
    internal static class KeyValueSecretsEngineV2Extensions
    {
        public static bool PathExists(this IKeyValueSecretsEngineV2 kv, string path, out Secret<ListInfo> o, string mountPoint = null)
        {
            o = null;

            try
            {
                o = kv.ReadSecretPathsAsync(path, mountPoint).Sync();
                return true;
            }
            catch (Exception ex)
            {
                if (ex is AggregateException agg) 
                    if (agg.GetBaseException() is not VaultApiException) 
                        ConfigurationLogging.Error(ex.ToString());

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

        public VaultConfigurationClient(string address, string token)
           => InitializeFields(address, new TokenAuthMethodInfo(token));

        public VaultConfigurationClient(string address, string roleId, string secretId)
            => InitializeFields(address, new AppRoleAuthMethodInfo(roleId, secretId));

        public VaultConfigurationClient(string address, (string username, string password) ldapUserNamePassword)
            => InitializeFields(address, new LDAPAuthMethodInfo(ldapUserNamePassword.username, ldapUserNamePassword.password));

        private void InitializeFields(string address, IAuthMethodInfo authMethod)
        {
            var settings = new VaultClientSettings(address, authMethod);
            var client = new VaultClient(settings);
            _kvV2 = client.V1.Secrets.KeyValue.V2;
            _token = client.V1.Auth.Token;
            _vaultClientRefreshTimer = new(RefreshToken, null, TimeSpan.FromHours(0.75), TimeSpan.FromHours(0.75));
        }

        private void RefreshToken(object s)
        {
            _tokenStr ??= _token.LookupSelfAsync().Sync().Data.Id.Substring(0, 6);


            _vaultClientRefreshTimer.Change(-1, -1);
            ConfigurationLogging.Info("Renewing vault client's token, '{0}...'", _tokenStr);
            _token.RenewSelfAsync().Wait();
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
            if (!_kvV2.PathExists($"{AppConfiguration}/{groupName}", out var paths, BaseMountPoint))
                return Array.Empty<ISetting>();

            return (from values in
                        from secret in
                            from path in
                                paths.Data.Keys
                            select _kvV2.ReadSecretAsync(
                                $"{AppConfiguration}/{groupName}/{path}",
                                mountPoint: BaseMountPoint
                            ).Sync()
                        select secret.Data.Data
                    select new Setting
                    {
                        Name = ((JsonElement)values["Name"]).GetString(),
                        Updated = ((JsonElement)values["Updated"]).GetDateTime(),
                        Value = ((JsonElement)values["Value"]).GetString()
                    }).Cast<ISetting>().ToArray();
        }

        public IReadOnlyCollection<ISetting> GetAllConnectionStrings(string groupName)
        {
            if (!_kvV2.PathExists($"{AppConfiguration}/{groupName}", out var paths, BaseMountPoint))
                return Array.Empty<ISetting>();

            return (from values in
                        from secret in
                            from path in
                                paths.Data.Keys
                            select _kvV2.ReadSecretAsync(
                                $"{AppConfiguration}/{groupName}/{path}", 
                                mountPoint: BaseMountPoint
                            ).Sync()
                        select secret.Data.Data
                    select new Setting
                    {
                        Name = ((JsonElement)values["Name"]).GetString(),
                        Updated = ((JsonElement)values["Updated"]).GetDateTime(),
                        Value = ((JsonElement)values["Value"]).GetString()
                    }).Cast<ISetting>().ToArray();
        }

        public void SetProperty(string groupName, string name, string value, DateTime updated)
        {
            var values = new Dictionary<string, object>
            {
                { "Name", name },
                { "Value", value },
                { "Updated", updated }
            };
            _kvV2.WriteSecretAsync($"{AppConfiguration}/{groupName}/{name}", values, mountPoint: BaseMountPoint);
        }

        private string _tokenStr;
        private Timer _vaultClientRefreshTimer;
        private IKeyValueSecretsEngineV2 _kvV2;
        private ITokenAuthMethod _token;
    }
}
