namespace Grid.Bot.ClientSettings;

using System;
using System.Text.RegularExpressions;

/// <summary>
/// Helper for client settings.
/// </summary>
public static partial class ClientSettingsNameHelper
{
    [GeneratedRegex(@"^([DS])?F(Flag|Log|Int|String)", RegexOptions.Compiled)]
    public static partial Regex PrefixedSettingRegex();

    /// <summary>
    /// Determines if the setting name is a filtered value setting.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <returns>True if the setting is a filtered setting, otherwise false.</returns>
    public static bool IsFilteredSetting(string name)
        => name.EndsWith(ClientSettingsFilteredValue<object>.PlaceFilterSuffix) || name.EndsWith(ClientSettingsFilteredValue<object>.DataCenterFilterSuffix);

    /// <summary>
    /// Extracts the filtered setting name and type from the given name.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <returns>A tuple containing the name of the setting and its type.</returns>
    public static (string name, ClientSettingsFilterType type) ExtractFilteredSettingName(string name)
    {
        var type = ClientSettingsFilterType.Place;
        
        if (name.EndsWith(ClientSettingsFilteredValue<object>.PlaceFilterSuffix))
        {
            name = name[..^ClientSettingsFilteredValue<object>.PlaceFilterSuffix.Length];
        }
        else if (name.EndsWith(ClientSettingsFilteredValue<object>.DataCenterFilterSuffix))
        {
            type = ClientSettingsFilterType.DataCenter;
            name = name[..^ClientSettingsFilteredValue<object>.DataCenterFilterSuffix.Length];
        }

        return (name, type);
    }

    /// <summary>
    /// Gets the type of setting from its name.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <returns>The type of the setting.</returns>
    public static ClientSettingType GetSettingTypeFromName(string name)
    {
        if (!PrefixedSettingRegex().IsMatch(name)) return ClientSettingType.String;

        // F = flag, I = int, S = string, L = log (int)
        // e.g:
        // FFlagTest
        // 012345678
        //  ^
        //
        // DFFlagTest
        // 0123456789
        //   ^

        var prefix = name[0];
        if (prefix != 'F')
            prefix = name[1..][1];
        else
            prefix = name[1];

        return prefix switch
        {
            'F' => ClientSettingType.Bool, // FFlag
            'I' or 'L' => ClientSettingType.Int, // FInt, FLog
            _ => ClientSettingType.String, // FString
        };

    }
}