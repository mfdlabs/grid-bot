namespace MFDLabs.Analytics.Google.MetricsProtocol.Client;

using System.Linq;
using System.Text.RegularExpressions;

using Text.Extensions;

internal static class RegexExtensions
{
    public static string RegexReplace(
        this string str,
        string pattern,
        string replacement,
        RegexOptions options = RegexOptions.None
    )
        => Regex.Replace(str, pattern, replacement, options);

    public static string RegexReplace(
        this string str,
        string pattern,
        MatchEvaluator eval,
        RegexOptions options = RegexOptions.None
    )
        => Regex.Replace(str, pattern, eval, options);

    public static string[] RegexSplit(
        this string str,
        string pattern,
        RegexOptions options = RegexOptions.None
    )
        => Regex.Split(str, pattern, options);
}

/// <summary>
/// Facilitates helpers for string manipulation and conversion.
/// </summary>
internal static class Converters
{
    /// <summary>
    /// Converts a string into a `snake_case` string.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The converted string.</returns>
    /// <example>
    /// var camelCase = "camelCase";
    /// var snakeCase = Converters.ToSnakeCase(camelCase);
    ///
    /// Console.WriteLine(snakeCase); // "camel_case"
    /// </example>
    public static string ToSnakeCase(this string str)
        => !str.IsNullOrEmpty()
            ? str.RegexReplace(/* language=regex */@"\W+", " ", RegexOptions.Compiled)
                .RegexSplit(/* language=regex */@" |\B(?=[A-Z])", RegexOptions.Compiled)
                .Select(w => w.ToLower())
                .Join('_')
            : str;

    /// <summary>
    /// Converts a string into a `camelCase` string.
    /// </summary>
    /// <param name="str">The string to convert.</param>
    /// <returns>The converted string.</returns>
    /// <example>
    /// var snakeCase = "snake_case";
    /// var camelCase = Converters.ToCamelCase(snakeCase);
    ///
    /// Console.WriteLine(camelCase); // "snakeCase"
    /// </example>
    public static string ToCamelCase(this string str)
        => !str.IsNullOrEmpty()
            ? str.RegexReplace(/* language=regex */@"(?:^\w|[A-Z]|\b\w|\s+)", match =>
            {
                if (match.Value.IsMatch(@"\s+")) return "";

                return match.Index == 0 ? match.Value.ToLower() : match.Value.ToUpper();
            })
            : str;
}
