namespace Grid.Bot.Extensions;

using System;
using System.ComponentModel;
using System.Collections.Generic;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using VaultSharp;

using Logging;

using Grid.ProcessManagement;
using Grid.ProcessManagement.Docker;

/// <summary>
/// Extension methods for registering the application configuration.
/// </summary>
public static class AppConfigurationExtensions
{
    /// <summary>
    /// Builds the application configuration and registers the migrated options.
    /// </summary>
    /// <remarks>
    /// Precedence, highest first: environment variables, appsettings.json, appsettings.{environment}.json,
    /// runtime writes (<see cref="ISettingsWriter"/>), Vault.
    /// Local settings always win over remote ones.
    /// Options bind from their section and then from the root, so unprefixed environment variables keep working.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddAppConfiguration(this IServiceCollection services)
    {
        // Later sources win, so these are added lowest precedence first.
        static IConfigurationBuilder AddLocalSources(IConfigurationBuilder builder) => builder
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile($"appsettings.{EnvironmentDataProvider.EnvironmentName}.json", optional: true)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        // The Vault address and credential are themselves settings, so read them before Vault is available.
        var bootstrap = AddLocalSources(new ConfigurationBuilder()).Build();
        var bootstrapOptions = new GlobalOptions();

        bootstrap.GetSection(GlobalOptions.SectionName).Bind(bootstrapOptions);
        bootstrap.Bind(bootstrapOptions);

        var builder = new ConfigurationBuilder();
        var runtime = new RuntimeConfigurationSource();

        IVaultClient vaultClient = null;

        if (!string.IsNullOrWhiteSpace(bootstrapOptions.VaultAddress))
        {
            vaultClient = new VaultFactory(Logger.Singleton)
                .CreateClient(bootstrapOptions.VaultAddress, bootstrapOptions.VaultCredential);

            builder.AddGridBotVault(vaultClient, Logger.Singleton);
        }

        builder.Add(runtime);

        var configuration = AddLocalSources(builder).Build();

        // Only environment variables bind from the root, so root-level JSON keys can't override a sectioned value.
        var environment = new ConfigurationBuilder().AddEnvironmentVariables().Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ISettingsWriter>(new SettingsWriter(
            vaultClient,
            EnvironmentDataProvider.VaultMountPath,
            runtime.Provider,
            Logger.Singleton
        ));

        void AddOptions<T>(string section) where T : class
        {
            services.Configure<T>(configuration.GetSection(section));
            services.Configure<T>(environment);
            services.PostConfigure<T>(options => ApplyCsvLists(options, environment, configuration.GetSection(section)));
        }

        AddOptions<GlobalOptions>(GlobalOptions.SectionName);
        AddOptions<BacktraceOptions>(BacktraceOptions.SectionName);
        AddOptions<FloodCheckerOptions>(FloodCheckerOptions.SectionName);
        AddOptions<CommandsOptions>(CommandsOptions.SectionName);
        AddOptions<ScriptsOptions>(ScriptsOptions.SectionName);

        AddOptions<DiscordOptions>(DiscordOptions.SectionName);
        services.AddSingleton<IValidateOptions<DiscordOptions>, DiscordOptionsValidator>();

        AddOptions<GridOptions>(GridOptions.SectionName);
        services.AddSingleton<IValidateOptions<GridOptions>, GridOptionsValidator>();
        services.AddSingleton<GridServerSettings>();
        services.AddSingleton<IGridServerDockerSettings>(provider => provider.GetRequiredService<GridServerSettings>());
        services.AddSingleton<IGridServerProcessSettings>(provider => provider.GetRequiredService<GridServerSettings>());

        AddOptions<GrpcOptions>(GrpcOptions.SectionName);
        services.AddSingleton<IValidateOptions<GrpcOptions>, GrpcOptionsValidator>();

        AddOptions<WebOptions>(WebOptions.SectionName);
        services.AddSingleton<IValidateOptions<WebOptions>, WebOptionsValidator>();
        services.PostConfigure<WebOptions>(options =>
        {
            if (options.WebServerAllowedProxyRanges is not { Length: > 0 })
                options.WebServerAllowedProxyRanges = WebOptions.DefaultAllowedProxyRanges;
        });

        return services;
    }

    // Lists and dictionaries were stored as flat strings, so accept those for any matching property.
    private static void ApplyCsvLists(object options, IConfiguration environment, IConfiguration section)
    {
        foreach (var property in options.GetType().GetProperties())
        {
            if (!property.CanWrite) continue;

            var isDictionary = property.PropertyType == typeof(IDictionary<string, string>) || property.PropertyType == typeof(Dictionary<string, string>);
            if (!property.PropertyType.IsArray && !isDictionary) continue;

            var raw = environment[property.Name] ?? section[property.Name];
            if (string.IsNullOrWhiteSpace(raw)) continue;

            if (isDictionary)
            {
                // One key=value pair per line.
                var dictionary = new Dictionary<string, string>();

                foreach (var line in raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var pair = line.Split('=');

                    if (pair.Length == 2)
                        dictionary[pair[0]] = pair[1];
                }

                property.SetValue(options, dictionary);

                continue;
            }

            var csv = raw;

            var elementType = property.PropertyType.GetElementType();
            var converter = TypeDescriptor.GetConverter(elementType);
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var array = Array.CreateInstance(elementType, parts.Length);

            for (var i = 0; i < parts.Length; i++)
                array.SetValue(converter.ConvertFromInvariantString(parts[i]), i);

            property.SetValue(options, array);
        }
    }
}
