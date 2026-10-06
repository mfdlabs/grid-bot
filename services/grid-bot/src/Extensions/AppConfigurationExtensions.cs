namespace Grid.Bot.Extensions;

using System;

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
        }

        AddOptions<GlobalOptions>(GlobalOptions.SectionName);

        AddOptions<GrpcOptions>(GrpcOptions.SectionName);
        services.AddSingleton<IValidateOptions<GrpcOptions>, GrpcOptionsValidator>();

        return services;
    }
}
