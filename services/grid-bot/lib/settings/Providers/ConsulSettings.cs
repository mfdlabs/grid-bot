namespace Grid.Bot;

using System;

using Configuration;

using IServiceDiscoverySettings = global::ServiceDiscovery.ISettings;

/// <summary>
/// Settings provider for all Consul related stuff.
/// </summary>
public class ConsulSettings : BaseSettingsProvider, IServiceDiscoverySettings
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.ConsulPath;

    /// <inheritdoc cref="IServiceDiscoverySettings.ConsulAddress"/>
    [SettingName("CONSUL_ADDR")]
    public string ConsulAddress => GetOrDefault(
        "CONSUL_ADDR", // Accom for local ENV var.
        "http://127.0.0.1:8500"
    );

    /// <inheritdoc cref="IServiceDiscoverySettings.ConsulBackoffBase"/>
    public TimeSpan ConsulBackoffBase => GetOrDefault(
        nameof(ConsulBackoffBase),
        TimeSpan.FromMilliseconds(1)
    );

    /// <inheritdoc cref="IServiceDiscoverySettings.ConsulLongPollingMaxWaitTime"/>
    public TimeSpan ConsulLongPollingMaxWaitTime => GetOrDefault(
        nameof(ConsulLongPollingMaxWaitTime),
        TimeSpan.FromMinutes(5)
    );

    /// <inheritdoc cref="IServiceDiscoverySettings.MaximumConsulBackoff"/>
    public TimeSpan MaximumConsulBackoff => GetOrDefault(
        nameof(MaximumConsulBackoff),
        TimeSpan.FromSeconds(30)
    );
}
