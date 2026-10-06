namespace Grid.Bot.Extensions;

using System;
using System.ComponentModel;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Logging;

/// <summary>
/// Extension methods for registering the application configuration.
/// </summary>
public static class AppConfigurationExtensions
{
    /// <summary>
    /// Builds the application configuration and registers the migrated options.
    /// </summary>
    /// <remarks>
    /// Precedence, highest first: environment variables, appsettings.json, appsettings.{environment}.json, Vault.
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

        if (!string.IsNullOrWhiteSpace(bootstrapOptions.VaultAddress))
        {
            var client = new VaultFactory(Logger.Singleton)
                .CreateClient(bootstrapOptions.VaultAddress, bootstrapOptions.VaultCredential);

            builder.AddGridBotVault(client, Logger.Singleton);
        }

        var configuration = AddLocalSources(builder).Build();

        // Only environment variables bind from the root, so root-level JSON keys can't override a sectioned value.
        var environment = new ConfigurationBuilder().AddEnvironmentVariables().Build();

        services.AddSingleton<IConfiguration>(configuration);

        void AddOptions<T>(string section) where T : class
        {
            services.Configure<T>(configuration.GetSection(section));
            services.Configure<T>(environment);
            services.PostConfigure<T>(options => ApplyCsvLists(options, environment, configuration.GetSection(section)));
        }

        AddOptions<GlobalOptions>(GlobalOptions.SectionName);

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

    // Lists were stored as comma separated values, so accept those for any array property.
    private static void ApplyCsvLists(object options, IConfiguration environment, IConfiguration section)
    {
        foreach (var property in options.GetType().GetProperties())
        {
            if (!property.CanWrite || !property.PropertyType.IsArray) continue;

            var csv = environment[property.Name] ?? section[property.Name];
            if (string.IsNullOrWhiteSpace(csv)) continue;

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
