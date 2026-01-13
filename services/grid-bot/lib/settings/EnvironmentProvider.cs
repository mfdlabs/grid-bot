namespace Grid.Bot;

using System;

/// <summary>
/// Provides the environment name for the settings provider.
/// </summary>
public static class EnvironmentProvider
{
    private const string ProviderEnvironmentNameEnvVar = "ENVIRONMENT";
    private const string NomadEnvironmentMetadataEnvVar = $"NOMAD_META_{ProviderEnvironmentNameEnvVar}";

    private const string DefaultEnvironmentName = "development";

    /// <summary>
    /// Gets the environment name for the settings provider.
    /// </summary>
    public static string EnvironmentName { get; } = Environment.GetEnvironmentVariable(ProviderEnvironmentNameEnvVar) 
                                                    ?? Environment.GetEnvironmentVariable(NomadEnvironmentMetadataEnvVar) 
                                                    ?? DefaultEnvironmentName;
}
