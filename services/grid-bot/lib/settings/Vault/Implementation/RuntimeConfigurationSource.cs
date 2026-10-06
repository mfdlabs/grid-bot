namespace Grid.Bot;

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

/// <summary>
/// A configuration source holding values written at runtime.
/// </summary>
public class RuntimeConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Gets the provider this source builds.
    /// </summary>
    public RuntimeConfigurationProvider Provider { get; } = new();

    /// <inheritdoc/>
    public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
}

/// <summary>
/// An in-memory configuration provider that raises a reload whenever a value is set.
/// </summary>
public class RuntimeConfigurationProvider : ConfigurationProvider
{
    private readonly object _lock = new();

    /// <inheritdoc cref="ConfigurationProvider.Set(string, string)"/>
    public override void Set(string key, string value)
    {
        lock (_lock)
        {
            // Copy on write so readers never see a partially updated dictionary.
            Data = new Dictionary<string, string>(Data, StringComparer.OrdinalIgnoreCase) { [key] = value };
        }

        OnReload();
    }
}
