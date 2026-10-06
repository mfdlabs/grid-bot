namespace Grid.Bot;

using System;

using Microsoft.Extensions.Configuration;

using VaultSharp;

using Logging;

/// <summary>
/// Grid-bot specific Vault configuration extensions.
/// </summary>
public static class GridBotVaultConfigurationExtensions
{
    private const string _refreshIntervalEnvVar = "DEFAULT_PROVIDER_REFRESH_INTERVAL";
    private static readonly TimeSpan _defaultRefreshInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Adds the grid-bot settings secrets from Vault, one section per migrated options class.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <param name="client">The Vault client.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddGridBotVault(this IConfigurationBuilder builder, IVaultClient client, ILogger logger = null)
        => builder.AddVault(client, EnvironmentDataProvider.VaultMountPath, source =>
        {
            source.Logger = logger;
            source.ReloadInterval = TimeSpan.TryParse(Environment.GetEnvironmentVariable(_refreshIntervalEnvVar), out var interval)
                ? interval
                : _defaultRefreshInterval;

            source.Map(GlobalOptions.SectionName, EnvironmentDataProvider.MapPath(GlobalOptions.VaultPath));
            source.Map(GrpcOptions.SectionName, EnvironmentDataProvider.MapPath(GrpcOptions.VaultPath));
            source.Map(WebOptions.SectionName, EnvironmentDataProvider.MapPath(WebOptions.VaultPath));
        });
}
