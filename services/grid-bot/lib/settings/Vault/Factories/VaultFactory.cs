namespace Grid.Bot;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.AppRole;

using Logging;

/// <summary>
/// Default <see cref="IVaultFactory"/>.
/// </summary>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="logger"/> cannot be null.</exception>
public class VaultFactory(ILogger logger) : IVaultFactory
{
    private const char _appRoleSplit = ':';
    private const string _defaultAppRoleMountPath = "approle";

    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static IAuthMethodInfo GetAuthMethodInfo(string credential)
    {
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

    /// <inheritdoc cref="IVaultFactory.CreateClient(string, string)"/>
    public IVaultClient CreateClient(string address, string credential)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(address));
        if (string.IsNullOrWhiteSpace(credential)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(credential));

        var client = new VaultClient(new(address, GetAuthMethodInfo(credential)));

        Task.Factory.StartNew(() => RefreshToken(client), TaskCreationOptions.LongRunning);

        return client;
    }

    private async Task RefreshToken(VaultClient client)
    {
        var token = (await client.V1.Auth.Token.LookupSelfAsync().ConfigureAwait(false))?.Data;

        if (token?.Renewable != true)
            return;

        var lease = token.TimeToLive;
        if (lease == 0) return;

        _logger.Debug("Setting up token refresh thread for vault client!");

        while (true)
        {
            await client.V1.Auth.Token.RenewSelfAsync().ConfigureAwait(false);

            Thread.Sleep(TimeSpan.FromSeconds(lease));
        }
    }
}
