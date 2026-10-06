namespace Grid.Bot;

using System;

using VaultSharp;

/// <summary>
/// A factory that creates Vault clients.
/// </summary>
public interface IVaultFactory
{
    /// <summary>
    /// Creates a Vault client for the specified address and credential.
    /// </summary>
    /// <param name="address">The Vault address.</param>
    /// <param name="credential">A Vault token, or <c>roleId:secretId[:mount]</c> for AppRole.</param>
    /// <returns>The Vault client.</returns>
    /// <exception cref="ArgumentException">
    /// - <paramref name="address"/> is <see langword="null"/> or whitespace.
    /// - <paramref name="credential"/> is <see langword="null"/> or whitespace.
    /// </exception>
    IVaultClient CreateClient(string address, string credential);
}
