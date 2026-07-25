namespace Vault;

using System;

using VaultSharp;

/// <summary>
/// A factory that provides a Vault client.
/// </summary>
/// <seealso cref="IVaultClient" />
public interface IVaultClientFactory
{
    /// <summary>
    /// Gets the global Vault client which is configured from the environment variables.
    /// </summary>
    /// <remarks>
    /// Before using this the following environment variables must be set:
    /// <list type="bullet">
    /// <item>VAULT_ADDR</item>
    /// <item>If using authentication: VAULT_TOKEN or VAULT_CREDENTIAL</item>
    /// </list>
    /// </remarks>
    /// <returns>The Vault client.</returns>
    /// <exception cref="ApplicationException">The environment variables are not set.</exception>
    IVaultClient GetClient();

    /// <summary>
    /// Gets the Vault for the specified address and credentials.
    /// </summary>
    /// <param name="address">The address.</param>
    /// <param name="credentials">The credentials. Can be null.</param>
    /// <returns>The Vault client.</returns>
    /// <exception cref="ArgumentException">
    /// - <paramref name="address"/> is <see langword="null" /> or whitespace.
    /// - <paramref name="credentials"/> is <see langword="null" /> or whitespace.
    /// </exception>
    IVaultClient GetClient(string address, string credentials);
}
