namespace Text.Extensions;

using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>
/// Extension methods regaring Text.
/// </summary>
public static class StringExtensions
{

    private static readonly JsonSerializerSettings _sharedJsonSerializerSettings = new()
    {
        Converters = new[]
        {
            new StringEnumConverter()
        },
        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateParseHandling = DateParseHandling.DateTime,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        NullValueHandling = NullValueHandling.Include,
        FloatFormatHandling = FloatFormatHandling.DefaultValue,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        FloatParseHandling = FloatParseHandling.Double
    };

    /// <inheritdoc cref="string.IsNullOrEmpty(string)"/>
    public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);

    /// <summary>
    /// Escapes the specified string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The escaped string.</returns>
    public static string Escape(this string s)
    {
        if (s == null) 
            return null;

        var builder = new StringBuilder();
        foreach (var c in s)
        {
            if (c > 127)
            {
                // This character is too big for ASCII
                var e = "\\u" + ((int)c).ToString("x4");
                builder.Append(e);
            }
            else
                builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns true if the string contains any unicode characters.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>True if the string contains unicode characters.</returns>
    public static bool ContainsUnicode(this string s) => s.Any(c => c > 255);

    /// <summary>
    /// Escapes all new lines in the string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The string.</returns>
    public static string EscapeNewLines(this string s) => s?.Replace("\r", "\\r")?.Replace("\n", "\\n");

    /// <summary>
    /// Escapes phone-specific quotes in the string.
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The string.</returns>
    public static string EscapeQuotes(this string s) => Regex.Replace(s, "[\"“‘”]", "\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <inheritdoc cref="string.Join(string, string[])"/>
    public static string Join<T>(this T[] arr, string seperator) => string.Join(seperator, arr);

    /// <inheritdoc cref="string.Join(string, string[])"/>
    public static string Join<T>(this T[] arr, char seperator) => arr.Join(seperator.ToString());

    /// <inheritdoc cref="string.Join(string, string[])"/>
    public static string Join<T>(this IEnumerable<T> arr, string seperator) => string.Join(seperator, arr);

    /// <summary>
    /// Converts the specified object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="self">The object.</param>
    /// <returns>The string.</returns>
    public static string ToJson<T>(this T self) => 
        JsonConvert.SerializeObject(
            self,
            Formatting.None,
            _sharedJsonSerializerSettings
        );

    /// <summary>
    /// Converts the specified object to a JSON string.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="self">The object.</param>
    /// <returns>The string.</returns>
    public static string ToJsonPretty<T>(this T self)
    {
        var parsedJson = JsonConvert.DeserializeObject(
            self.ToJson(),
            _sharedJsonSerializerSettings
        );

        return JsonConvert.SerializeObject(
            parsedJson,
            Formatting.Indented,
            _sharedJsonSerializerSettings
        );
    }

    /// <inheritdoc cref="Regex.Match(string, string, RegexOptions)"/>
    public static bool IsMatch(this string s, string pattern, RegexOptions options = RegexOptions.None) => Regex.IsMatch(s, pattern, options);
    
    private static Match GetCodeBlockMatch(this string s) => Regex.Match(s, @"```(.*?)\s(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline);

    /// <summary>
    /// Gets the contents of a code block like:
    /// <br />
    /// <br />
    /// ```<br />
    /// contents<br />
    /// ```<br />
    /// </summary>
    /// <param name="s">The string.</param>
    /// <returns>The contents.</returns>
    public static string GetCodeBlockContents(this string s)
    {
        var match = s.GetCodeBlockMatch();

        if (match != null && match.Groups.Count == 3)
        {
            if (!s.Contains($"```{match.Groups[1].Value}\n")) 
                return $"{match.Groups[1].Value} {match.Groups[2].Value}";

            return match.Groups[2].Value;
        }

        return s.Replace("`", ""); // Return the value here again?
    }
}
