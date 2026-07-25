namespace Configuration;

using System;

/// <summary>
/// Represents a <see cref="IConfigurationProvider"/> backed by Vault.
/// </summary>
public interface IVaultProvider : IConfigurationProvider, IDisposable
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
}
