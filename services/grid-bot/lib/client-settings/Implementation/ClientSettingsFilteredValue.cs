namespace Grid.Bot.ClientSettings;

using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Represents a filtered value.
/// </summary>
public struct ClientSettingsFilteredValue<T>
{
    private const char _filterDelimiter = ';';

    /// <summary>
    /// Suffix for place filters.
    /// </summary>
    public const string PlaceFilterSuffix = "_PlaceFilter";

    /// <summary>
    /// Suffix for datacenter filters.
    /// </summary>
    public const string DataCenterFilterSuffix = "_DataCenterFilter";

    /// <summary>
    /// Construct a new instance of <see cref="ClientSettingsFilteredValue{T}"/>
    /// </summary>
    public ClientSettingsFilteredValue()
    {
    }

    /// <summary>
    /// Get or set the raw value.
    /// </summary>
    public T Value { get; set; }

     /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets the type of the setting.
    /// </summary>
    public readonly ClientSettingType Type
    {
        get
        {
            return Value switch
            {
                bool => ClientSettingType.Bool,
                long => ClientSettingType.Int,
                _ => ClientSettingType.String,
            };
        }
    }

    /// <summary>
    /// Gets or sets the type of filter.
    /// </summary>
    public ClientSettingsFilterType FilterType { get; set; }

    /// <summary>
    /// Gets the filtered place IDs or datacenter IDs.
    /// </summary>
    public HashSet<long> FilteredIds { get; private set; } = [];

    /// <summary>
    /// Implicit conversion of <see cref="ClientSettingsFilteredValue{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="value">The current <see cref="ClientSettingsFilteredValue{T}"/></param>
    public static implicit operator T(ClientSettingsFilteredValue<T> value) => value.Value;

    /// <summary>
    /// Converts the string representation of the filtered value to a filtered value.
    /// </summary>
    /// <param name="name">The raw name of the setting, used to determine the type of filter.</param>
    /// <param name="value">The string value of the setting.</param>
    /// <returns>A new filtered value.</returns>
    public static ClientSettingsFilteredValue<T> FromString(string name, string value)
    {
        if (!ClientSettingsNameHelper.IsFilteredSetting(name))
            throw new ArgumentException($"The setting name does not end with {PlaceFilterSuffix} or {DataCenterFilterSuffix}!", nameof(name));

        var filterType = name.EndsWith(PlaceFilterSuffix)
            ? ClientSettingsFilterType.Place
            : ClientSettingsFilterType.DataCenter;
        var settingName = filterType == ClientSettingsFilterType.Place
            ? name[..^PlaceFilterSuffix.Length]
            : name[..^DataCenterFilterSuffix.Length];
        var settingType = ClientSettingsNameHelper.GetSettingTypeFromName(name);

        var entries = value.Split(_filterDelimiter).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        if (entries.Count == 0) throw new ArgumentException("Value had no entries!", nameof(value));

        var settingValueRaw = entries.First();
        var filteredIds = entries.Skip(1).Select(long.Parse);
        object settingValue = settingType switch
        {
            ClientSettingType.Bool => bool.Parse(settingValueRaw),
            ClientSettingType.Int => long.Parse(settingValueRaw),
            _ => settingValueRaw,
        };

        return new ClientSettingsFilteredValue<T>
        {
            Name = settingName,
            FilterType = filterType,
            FilteredIds = [.. filteredIds],
            Value = (T)(object)settingValue.ToString()
        };
    }

    /// <summary>
    /// Convert the filtered value to a string.
    /// </summary>
    /// <returns>The new name and the stringified value.</returns>
    public new readonly (string key, string value) ToString()
    {
        var name = $"{Name}_{FilterType}Filter";
        ICollection<string> values = [Value.ToString(), ..FilteredIds.Select(x => x.ToString())];

        return (name, string.Join(_filterDelimiter, values));
    }
}
