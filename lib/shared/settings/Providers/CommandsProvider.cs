namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for all commands related stuff.
/// </summary>
public class CommandsSettings : BaseSettingsProvider<CommandsSettings>
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.CommandsPath;

    /// <summary>
    /// Should the command registry be registered at startup?
    /// </summary>
    public bool RegisterCommandRegistryAtAppStart => GetOrDefault(
        nameof(RegisterCommandRegistryAtAppStart),
        true
    );

    /// <summary>
    /// Gets the command prefix.
    /// </summary>
    public string Prefix => GetOrDefault(
        nameof(Prefix),
        ">"
    );

    /// <summary>
    /// Gets the retry interval for the command circuit breaker.
    /// </summary>
    public TimeSpan CommandCircuitBreakerWrapperRetryInterval => GetOrDefault(
        nameof(CommandCircuitBreakerWrapperRetryInterval),
        TimeSpan.FromSeconds(10)
    );
}
