namespace Grid.Bot.Utility;

using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Represents a filtered value.
/// </summary>
public struct FilteredValue<T>
{
    private const char FilterDelimiter = ';';

    /// <summary>
    /// Suffix for place filters.
    /// </summary>
    public const string PlaceFilterSuffix = "_PlaceFilter";

    /// <summary>
    /// Suffix for datacenter filters.
    /// </summary>
    public const string DataCenterFilterSuffix = "_DataCenterFilter";

    /// <summary>
    /// Construct a new instance of <see cref="FilteredValue{T}"/>
    /// </summary>
    public FilteredValue()
    {
    }

    /// <summary>
    /// Get or set the raw value.
    /// </summary>
    public T Value { get; set; }

     /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Gets the type of the setting.
    /// </summary>
    public readonly SettingType Type
    {
        get
        {
            return Value switch
            {
                bool => SettingType.Bool,
                long => SettingType.Int,
                _ => SettingType.String,
            };
        }
    }

    /// <summary>
    /// Gets or sets the type of filter.
    /// </summary>
    public FilterType FilterType { get; set; }

    /// <summary>
    /// Gets the filtered place IDs or datacenter IDs.
    /// </summary>
    public HashSet<long> FilteredIds { get; private init; } = [];

    /// <summary>
    /// Implicit conversion of <see cref="FilteredValue{T}"/> to <typeparamref name="T"/>
    /// </summary>
    /// <param name="value">The current <see cref="FilteredValue{T}"/></param>
    public static implicit operator T(FilteredValue<T> value) => value.Value;

    /// <summary>
    /// Converts the string representation of the filtered value to a filtered value.
    /// </summary>
    /// <param name="name">The raw name of the setting, used to determine the type of filter.</param>
    /// <param name="value">The string value of the setting.</param>
    /// <returns>A new filtered value.</returns>
    public static FilteredValue<T> FromString(string name, string value)
    {
        if (!ClientSettingsNameHelper.IsFilteredSetting(name))
            throw new ArgumentException($"The setting name does not end with {PlaceFilterSuffix} or {DataCenterFilterSuffix}!", nameof(name));

        var filterType = name.EndsWith(PlaceFilterSuffix)
            ? FilterType.Place
            : FilterType.DataCenter;
        var settingName = filterType == FilterType.Place
            ? name[..^PlaceFilterSuffix.Length]
            : name[..^DataCenterFilterSuffix.Length];
        var settingType = ClientSettingsNameHelper.GetSettingTypeFromName(name);

        var entries = value.Split(FilterDelimiter).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
        if (entries.Count == 0) throw new ArgumentException("Value had no entries!", nameof(value));

        var settingValueRaw = entries.First();
        var filteredIds = entries.Skip(1).Select(long.Parse);
        object settingValue = settingType switch
        {
            SettingType.Bool => bool.Parse(settingValueRaw),
            SettingType.Int => long.Parse(settingValueRaw),
            _ => settingValueRaw,
        };

        return new FilteredValue<T>
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

        return (name, string.Join(FilterDelimiter, values));
    }
}
