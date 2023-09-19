namespace Grid.Bot;

using System;

/// <summary>
/// Settings provider for the exception based auto moderator.
/// </summary>
public class ExceptionBasedAutomoderatorSettings : BaseSettingsProvider<ExceptionBasedAutomoderatorSettings>
{
    /// <inheritdoc cref="Configuration.IVaultProvider.Path"/>
    public override string Path => SettingsProvidersDefaults.ExceptionBasedAutomoderatorPath;

    /// <summary>
    /// Is the auto moderator enabled?
    /// </summary>
    public bool ExceptionBasedAutomoderatorEnabled => GetOrDefault(
        nameof(ExceptionBasedAutomoderatorEnabled),
        false
    );

    /// <summary>
    /// Gets the additional lease time.
    /// </summary>
    public TimeSpan ExceptionBasedAutomoderatorLeaseTimeSpanAddition => GetOrDefault(
        nameof(ExceptionBasedAutomoderatorLeaseTimeSpanAddition),
        TimeSpan.FromHours(1)
    );

    /// <summary>
    /// Gets the max amount of exceptions before a blacklist.
    /// </summary>
    public int ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist => GetOrDefault(
        nameof(ExceptionBasedAutomoderatorMaxExceptionHitsBeforeBlacklist),
        10
    );
}
