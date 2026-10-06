namespace Grid.Bot;

using System;
using System.Collections.Generic;

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
    /// The Vault secret path for each migrated options section.
    /// </summary>
    public static IReadOnlyDictionary<string, string> SectionPaths { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [GlobalOptions.SectionName] = EnvironmentDataProvider.MapPath(GlobalOptions.VaultPath),
        [GrpcOptions.SectionName] = EnvironmentDataProvider.MapPath(GrpcOptions.VaultPath),
        [WebOptions.SectionName] = EnvironmentDataProvider.MapPath(WebOptions.VaultPath),
        [BacktraceOptions.SectionName] = EnvironmentDataProvider.MapPath(BacktraceOptions.VaultPath),
        [FloodCheckerOptions.SectionName] = EnvironmentDataProvider.MapPath(FloodCheckerOptions.VaultPath),
        [CommandsOptions.SectionName] = EnvironmentDataProvider.MapPath(CommandsOptions.VaultPath),
        [DiscordOptions.SectionName] = EnvironmentDataProvider.MapPath(DiscordOptions.VaultPath),
        [GridOptions.SectionName] = EnvironmentDataProvider.MapPath(GridOptions.VaultPath),
        [ScriptsOptions.SectionName] = EnvironmentDataProvider.MapPath(ScriptsOptions.VaultPath),
        [AvatarOptions.SectionName] = EnvironmentDataProvider.MapPath(AvatarOptions.VaultPath),
        [ClientSettingsOptions.SectionName] = EnvironmentDataProvider.MapPath(ClientSettingsOptions.VaultPath),
        [MaintenanceOptions.SectionName] = EnvironmentDataProvider.MapPath(MaintenanceOptions.VaultPath),
        [DiscordRolesOptions.SectionName] = EnvironmentDataProvider.MapPath(DiscordRolesOptions.VaultPath),
    };

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

            foreach (var (section, path) in SectionPaths)
                source.Map(section, path);
        });
}
