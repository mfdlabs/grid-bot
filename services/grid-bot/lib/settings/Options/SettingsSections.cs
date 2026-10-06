namespace Grid.Bot;

using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// A migrated options section.
/// </summary>
/// <param name="Name">The configuration section name.</param>
/// <param name="OptionsType">The options class bound from the section.</param>
public sealed record SettingsSection(string Name, Type OptionsType);

/// <summary>
/// All migrated options sections.
/// </summary>
public static class SettingsSections
{
    private static readonly Dictionary<string, SettingsSection> _sections = new SettingsSection[]
    {
        new(GlobalOptions.SectionName, typeof(GlobalOptions)),
        new(GrpcOptions.SectionName, typeof(GrpcOptions)),
        new(WebOptions.SectionName, typeof(WebOptions)),
        new(BacktraceOptions.SectionName, typeof(BacktraceOptions)),
        new(FloodCheckerOptions.SectionName, typeof(FloodCheckerOptions)),
        new(CommandsOptions.SectionName, typeof(CommandsOptions)),
        new(DiscordOptions.SectionName, typeof(DiscordOptions)),
        new(GridOptions.SectionName, typeof(GridOptions)),
        new(ScriptsOptions.SectionName, typeof(ScriptsOptions)),
        new(AvatarOptions.SectionName, typeof(AvatarOptions)),
        new(ClientSettingsOptions.SectionName, typeof(ClientSettingsOptions)),
        new(MaintenanceOptions.SectionName, typeof(MaintenanceOptions)),
        new(DiscordRolesOptions.SectionName, typeof(DiscordRolesOptions)),
    }.ToDictionary(section => section.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets every section.
    /// </summary>
    public static IEnumerable<SettingsSection> All => _sections.Values;

    /// <summary>
    /// Finds a section by name, ignoring case.
    /// </summary>
    /// <param name="name">The section name.</param>
    /// <param name="section">The section, if found.</param>
    /// <returns><see langword="true"/> if the section exists.</returns>
    public static bool TryGet(string name, out SettingsSection section)
    {
        section = null;

        return name != null && _sections.TryGetValue(name, out section);
    }
}
