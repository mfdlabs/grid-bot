namespace FloodCheckers.Redis;

using System;

using Configuration;

internal class Settings : EnvironmentProvider, ISettings
{
    internal static readonly Settings Singleton = new Settings();

    /// <inheritdoc cref="ISettings.FloodCheckerRedisEndpointsCsv"/>
    public string FloodCheckerRedisEndpointsCsv => GetOrDefault(nameof(FloodCheckerRedisEndpointsCsv), string.Empty);

    /// <inheritdoc cref="ISettings.FloodCheckerMinimumWindowPeriod"/>
    public TimeSpan FloodCheckerMinimumWindowPeriod => GetOrDefault(nameof(FloodCheckerRedisEndpointsCsv), TimeSpan.FromSeconds(1));
}
