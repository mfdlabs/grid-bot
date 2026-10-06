namespace Grid.Bot;

using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

/// <summary>
/// Formats option values as the flat strings stored in Vault and environment variables.
/// </summary>
public static class SettingsValueFormatter
{
    /// <summary>
    /// Formats <paramref name="value"/>. Lists are comma separated and dictionaries are one key=value pair per line.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The flat string, empty for null.</returns>
    public static string Format(object value) => value switch
    {
        null => string.Empty,
        string text => text,
        Array array => string.Join(',', array.Cast<object>()),
        IDictionary<string, string> dictionary => string.Join('\n', dictionary.Select(pair => $"{pair.Key}={pair.Value}")),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };
}
