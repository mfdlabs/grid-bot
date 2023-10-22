﻿namespace Configuration;

using VaultSharp;

/// <summary>
/// Represents a <see cref="IConfigurationProvider"/> backed by Vault.
/// </summary>
public interface IVaultProvider : IConfigurationProvider
{
    /// <summary>
    /// Gets the mount path.
    /// </summary>
    string Mount { get; }

    /// <summary>
    /// Gets the path.
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Refreshes the current cached settings.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Applies the current cached values to the Vault server.
    /// </summary>
    /// <remarks>
    /// Please take care when calling this directly, as this will overwrite the secret!
    /// </remarks>
    void ApplyCurrent();

    /// <summary>
    /// Sets the <see cref="IVaultClient"/>.
    /// </summary>
    /// <remarks>
    /// If <see langword="null" />, will abort the current refresh thread.
    /// </remarks>
    /// <param name="client">The <see cref="IVaultClient"/></param>
    /// <param name="doRefresh">If true, initial refresh occurs and the refresh thread will start.</param>
    void SetClient(IVaultClient client = null, bool doRefresh = true);
}
