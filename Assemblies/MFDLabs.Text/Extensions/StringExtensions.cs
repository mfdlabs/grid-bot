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
        public static string EscapeNewLines(this string s) => s?.Replace("\r", "\\r")?.Replace("\n", "\\n");
        public static string EscapeQuotes(this string s) => Regex.Replace(s, "[\"“‘”]", "\"", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static string Join<T>(this T[] arr, string seperator) => string.Join(seperator, arr);
        public static string Join<T>(this T[] arr, char seperator) => arr.Join(seperator.ToString());
        public static string Join<T>(this ICollection<T> arr, string seperator) => string.Join(seperator, arr);
        public static string Join<T>(this ICollection<T> arr, char seperator) => arr.Join(seperator.ToString());
        public static string Join<T>(this IEnumerable<T> arr, string seperator) => string.Join(seperator, arr);
        public static string Join<T>(this IEnumerable<T> arr, char seperator) => arr.Join(seperator.ToString());
        public static string ToJson<T>(this T self) => TextGlobal.Singleton.SerializeJsonWithEnumConverter(self);
        public static string MakeFileSafeString(this string s) => Regex.Replace(s, @"[^a-z0-9_-]", "_", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>
        /// Thanks @Auxority#5441 for the Regex
        /// </summary>
        public static Match GetCodeBlockMatch(this string s) => Regex.Match(s, @"```(.*?)\s(.*?)```", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        public static string GetCodeBlockContents(this string s)
        {
            var match = s.GetCodeBlockMatch();
            if (match != null && match.Groups.Count == 3) return match.Groups[2].Value;
            return s; // Return the value here again?
        }
        public static string GetCodeBlockSyntaxType(this string s)
        {
            var match = s.GetCodeBlockMatch();
            if (match != null && match.Groups.Count == 3) return match.Groups[1].Value;
            return null; // Return the value here again?
        }
        /*public static string Replace(this string s, string oldValue, string newValue) => s.Replace(oldValue, newValue);
        public static string Replace(this string s, char oldChar, char newChar) => s.Replace(oldChar, newChar);*/
    }
}
