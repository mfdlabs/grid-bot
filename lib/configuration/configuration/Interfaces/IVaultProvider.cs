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
    /// Determines whether the write operation should happen immediately
    /// when Set is called or if it should be deferred until ApplyCurrent is called.
    /// </summary>
    bool AutomaticWrite { get; set; }

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
    /// Begins a write operation to the Vault server.
    /// 
    /// When the returned <see cref="IDisposable"/> is disposed, 
    /// <see cref="ApplyCurrent"/> will be called to apply the current cached values to the Vault server.
    /// </summary>
    /// <remarks>
    /// Only run this if <see cref="AutomaticWrite"/> is set to false, otherwise this will be a no-op.
    /// </remarks>
    /// <returns>An <see cref="IDisposable"/> that will call <see cref="ApplyCurrent"/> when disposed.</returns>
    IDisposable BeginTransaction();
}
