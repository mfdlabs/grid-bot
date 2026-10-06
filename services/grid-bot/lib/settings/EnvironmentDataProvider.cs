namespace Grid.Bot;

using System;

/// <summary>
/// Provides the environment name and Vault mount for the settings.
/// </summary>
public static class EnvironmentDataProvider
{
    private const string _providerEnvironmentNameEnvVar = "ENVIRONMENT";
    private const string _nomadEnvironmentMetadataEnvVar = $"NOMAD_META_{_providerEnvironmentNameEnvVar}";

    private const string _providerVaultMountEnvVar = "VAULT_MOUNT";

    private const string _defaultEnvironmentName = "development";
    private static readonly string _providerEnvironmentName =
           Environment.GetEnvironmentVariable(_providerEnvironmentNameEnvVar)
        ?? Environment.GetEnvironmentVariable(_nomadEnvironmentMetadataEnvVar)
        ?? _defaultEnvironmentName;

    private const string _defaultVaultMount = "grid-bot-settings";
    private static readonly string _providerVaultMount =
           Environment.GetEnvironmentVariable(_providerVaultMountEnvVar)
        ?? _defaultVaultMount;

    /// <summary>
    /// Gets the environment name for the settings.
    /// </summary>
    public static string EnvironmentName => _providerEnvironmentName;

    /// <summary>
    /// Gets the vault mount path for the settings.
    /// </summary>
    public static string VaultMountPath => _providerVaultMount;

    /// <summary>
    /// Maps a relative path to the full path within the vault environment.
    /// </summary>
    /// <param name="relativePath">The relative path within the vault environment.</param>
    /// <returns>The full path within the vault environment.</returns>
    public static string MapPath(string relativePath) => $"{EnvironmentName}/{relativePath}";
}