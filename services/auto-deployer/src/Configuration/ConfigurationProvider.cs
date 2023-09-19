#if USE_VAULT_SETTINGS_PROVIDER

namespace Grid.AutoDeployer;

using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.AppRole;

using Logging;
using Configuration;

/// <summary>
/// Provider for the Vault client used by configuration.
/// </summary>
public static class ConfigurationProvider
{
    private const string _vaultTokenEnvVar = "VAULT_TOKEN";
    private const string _vaultAddressEnvVar = "VAULT_ADDR";
    private const string _vaultCredentialEnvVar = "VAULT_CREDENTIAL";

    private const char _appRoleSplit = ':';
    private const string _defaultAppRoleMountPath = "approle";

    private const string _providerSingletonFieldName = "Singleton";

    /// <summary>
    /// A collection of all registerd configuration providers in this domain.
    /// </summary>
    public static ICollection<IConfigurationProvider> RegisteredProviders { get; private set; }

    /// <summary>
    /// Set up vault for the configuration.
    /// </summary>
    public static void SetUpVault()
    {
        var vaultAddr = Environment.GetEnvironmentVariable(_vaultAddressEnvVar);
        var vaultCredential = Environment.GetEnvironmentVariable(_vaultCredentialEnvVar)
                           ?? Environment.GetEnvironmentVariable(_vaultTokenEnvVar);

        if (string.IsNullOrEmpty(vaultCredential)) return;

        var authMethod = GetAuthMethodInfo(vaultCredential);

        var client = new VaultClient(new(vaultAddr, authMethod));

        Task.Factory.StartNew(() => RefreshToken(client), TaskCreationOptions.LongRunning);

        ApplyClients(client);
    }

    private static void RefreshToken(IVaultClient client)
    {
        Logger.Singleton.Information("Setting up token refresh thread for vault client!");

        while (true)
        {
            client.V1.Auth.Token.RenewSelfAsync().Wait();

            Thread.Sleep(SettingsProvidersDefaults.VaultClientTokenRefreshInterval);
        }
    }

    private static IAuthMethodInfo GetAuthMethodInfo(string credential)
    {
        if (credential.Contains(_appRoleSplit))
        {
            Logger.Singleton.Information("Using AppRole authentication for Vault!");

            var parts = credential.Split(_appRoleSplit);
            var roleId = parts.ElementAt(0);
            var secretId = parts.ElementAt(1);

            var mount = parts.ElementAtOrDefault(2) ?? _defaultAppRoleMountPath;

            return new AppRoleAuthMethodInfo(mount, roleId, secretId);
        }

        Logger.Singleton.Information("Using Token authentication for Vault!");

        return new TokenAuthMethodInfo(credential);
    }

    private static void ApplyClients(IVaultClient client)
    {
        RegisteredProviders = new List<IConfigurationProvider>();

        var ns = typeof(ConfigurationProvider).Namespace;
        var singletons = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => string.Equals(t.Namespace, ns, StringComparison.Ordinal) &&
                        t.BaseType.Name == typeof(BaseSettingsProvider<>).Name) // finicky
            .Select(t =>
            {
                var field = t.BaseType.GetField(_providerSingletonFieldName, BindingFlags.Static | BindingFlags.Public);
                if (field == null)
                {
                    Logger.Singleton.Warning("Provider {0} did not expose a public static field called Singleton!", t.FullName);

                    return null;
                }

                return (BaseSettingsProvider)field.GetValue(null);
            });

        foreach (var singleton in singletons)
        {
            if (singleton == null) continue;

            singleton.SetLogger(Logger.Singleton);
            singleton.SetClient(client);

            RegisteredProviders.Add(singleton);
        }
    }
}

#endif
