namespace Vault;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.AppRole;

using Logging;
using Threading.Extensions;

/// <summary>
/// A factory that provides a Vault client.
/// </summary>
/// <seealso cref="IVaultClient" />
/// <seealso cref="IVaultClientFactory" />
public class VaultClientFactory : IVaultClientFactory
{
    private const string _vaultTokenEnvVar = "VAULT_TOKEN";
    private const string _vaultAddressEnvVar = "VAULT_ADDR";
    private const string _vaultCredentialEnvVar = "VAULT_CREDENTIAL";

    private const char _appRoleSplit = ':';
    private const string _defaultAppRoleMountPath = "approle";

    private static readonly object _instanceLock = new();
    private static VaultClientFactory _instance = new();

    /// <summary>
    /// Gets the singleton instance of the <see cref="VaultClientFactory"/>.
    /// </summary>
    public static VaultClientFactory Singleton
    {
        get
        {
            if (_instance == null)
            {
                lock (_instanceLock)
                    _instance ??= new();
            }

            return _instance;
        }
    }

    private readonly object _globalClientLock = new();
    private IVaultClient _cachedGlobalClient;

    private static IAuthMethodInfo GetAuthMethodInfo(string credential)
    {
        if (string.IsNullOrWhiteSpace(credential)) return null;

        if (credential.Contains(_appRoleSplit))
        {
            var parts = credential.Split(_appRoleSplit);
            var roleId = parts.ElementAt(0);
            var secretId = parts.ElementAt(1);

            var mount = parts.ElementAtOrDefault(2) ?? _defaultAppRoleMountPath;

            return new AppRoleAuthMethodInfo(mount, roleId, secretId);
        }

        return new TokenAuthMethodInfo(credential);
    }


    /// <inheritdoc cref="IVaultClientFactory.GetClient()"/>
    public IVaultClient GetClient()
    {
        if (_cachedGlobalClient != null)
            return _cachedGlobalClient;

        lock (_globalClientLock)
        {
            if (_cachedGlobalClient != null)
                return _cachedGlobalClient;

            var vaultAddr = Environment.GetEnvironmentVariable(_vaultAddressEnvVar);
            var vaultCredential = Environment.GetEnvironmentVariable(_vaultCredentialEnvVar)
                               ?? Environment.GetEnvironmentVariable(_vaultTokenEnvVar);

            if (string.IsNullOrWhiteSpace(vaultAddr)) return null;
            if (string.IsNullOrWhiteSpace(vaultCredential)) throw new ApplicationException($"Required environment variable not found: {_vaultCredentialEnvVar} or {_vaultTokenEnvVar}");

            return _cachedGlobalClient ??= GetClient(vaultAddr, vaultCredential);
        }
    }

    /// <inheritdoc cref="IVaultClientFactory.GetClient(string, string)"/>
    public IVaultClient GetClient(string address, string credentials)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
        if (string.IsNullOrWhiteSpace(credentials)) throw new ArgumentNullException("Value cannot be null or whitespace.", nameof(credentials));

        var authMethod = GetAuthMethodInfo(credentials);

        var client = new VaultClient(new(address, authMethod));

        Task.Factory.StartNew(() => RefreshToken(client), TaskCreationOptions.LongRunning);

        return client;
    }

    private static void RefreshToken(IVaultClient client)
    {
        var token = client.V1.Auth.Token.LookupSelfAsync().Sync()?.Data;

        // Check if the client has a lease
        if (token?.Renewable != true)
            return;

        // Get the lease
        var lease = token?.TimeToLive;
        if (lease == null || lease == 0) return; // If it doesn't need to be refreshed

        Logger.Singleton.Debug("Setting up token refresh thread for vault client!");

        while (true)
        {
            client.V1.Auth.Token.RenewSelfAsync().Wait();

            Thread.Sleep(TimeSpan.FromSeconds(lease.Value));
        }
    }
}
