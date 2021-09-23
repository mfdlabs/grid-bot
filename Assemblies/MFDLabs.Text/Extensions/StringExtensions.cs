using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MFDLabs.Text.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);
        public static bool IsNullWhiteSpaceOrEmpty(this string str) => str.IsNullOrEmpty() || str.IsNullOrWhiteSpace();
        public static string Escape(this string s) => TextGlobal.Singleton.EscapeString(s);
        public static string Unescape(this string s) => TextGlobal.Singleton.UnescapeString(s);
        public static string Fill(this string s, char c, int n, TextGlobal.StringDirection direction) => TextGlobal.Singleton.FillString(s, c, n, direction);
        public static bool ContainsUnicode(this string s) => TextGlobal.Singleton.StringContainsUnicode(s);
        public static string EscapeNewLines(this string s) => s?.Replace("\n", "\\n");
        public static string EscapeQuotes(this string s) => Regex.Replace(s, "[\"“‘”]", "\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string Join<T>(this T[] arr, string seperator) => string.Join(seperator, arr);
        public static string Join<T>(this T[] arr, char seperator) => arr.Join(seperator.ToString());
        public static string Join<T>(this IList<T> arr, string seperator) => string.Join(seperator, arr);
        public static string Join<T>(this IList<T> arr, char seperator) => arr.Join(seperator.ToString());
        public static string ToJson<T>(this T self) => TextGlobal.Singleton.SerializeJsonWithEnumConverter(self);
    }
}
