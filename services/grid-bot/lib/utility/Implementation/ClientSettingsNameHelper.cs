namespace Grid.Bot.Utility;

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
        => name.EndsWith(FilteredValue<object>.PlaceFilterSuffix) || name.EndsWith(FilteredValue<object>.DataCenterFilterSuffix);

    /// <summary>
    /// Extracts the filtered setting name and type from the given name.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <returns>A tuple containing the name of the setting and its type.</returns>
    public static (string name, FilterType type) ExtractFilteredSettingName(string name)
    {
        var type = FilterType.Place;
        
        if (name.EndsWith(FilteredValue<object>.PlaceFilterSuffix))
        {
            name = name[..^FilteredValue<object>.PlaceFilterSuffix.Length];
        }
        else if (name.EndsWith(FilteredValue<object>.DataCenterFilterSuffix))
        {
            type = FilterType.DataCenter;
            name = name[..^FilteredValue<object>.DataCenterFilterSuffix.Length];
        }

        return (name, type);
    }

    /// <summary>
    /// Gets the type of setting from its name.
    /// </summary>
    /// <param name="name">The name of the setting.</param>
    /// <returns>The type of the setting.</returns>
    public static SettingType GetSettingTypeFromName(string name)
    {
        if (!PrefixedSettingRegex().IsMatch(name)) return SettingType.String;

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
        prefix = prefix != 'F' ? name[1..][1] : name[1];

        return prefix switch
        {
            'F' => SettingType.Bool, // FFlag
            'I' or 'L' => SettingType.Int, // FInt, FLog
            _ => SettingType.String, // FString
        };

    }
}
