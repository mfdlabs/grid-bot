namespace Grid.Bot;

using System;

/// <summary>
/// Provides the environment name for the settings provider.
/// </summary>
public static class EnvironmentProvider
{
    private const string _providerEnvironmentNameEnvVar = "ENVIRONMENT";
    private const string _nomadEnvironmentMetadataEnvVar = $"NOMAD_META_{_providerEnvironmentNameEnvVar}";

    private const string _defaultEnvironmentName = "development";
    private static readonly string _providerEnvironmentName = 
           Environment.GetEnvironmentVariable(_providerEnvironmentNameEnvVar) 
        ?? Environment.GetEnvironmentVariable(_nomadEnvironmentMetadataEnvVar) 
        ?? _defaultEnvironmentName;

    /// <summary>
    /// Gets the environment name for the settings provider.
    /// </summary>
    public static string EnvironmentName => _providerEnvironmentName;
}