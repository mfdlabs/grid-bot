using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Text
{
    public sealed class TextGlobal
    {
        public enum StringDirection
        {
            Left,
            Right
        }

        public static string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0) return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string FillString(string s, char c, int n, StringDirection d)
        {
            switch (d)
            {
                case StringDirection.Left:
                    s = new string(c, n) + s;
                    break;
                case StringDirection.Right:
                    s += new string(c, n);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(d), d, null);
            }
            return s;
        }

        public static string EscapeString(string s)
        {
            if (s == null) return null;
            var b = new StringBuilder();
            foreach (var c in s)
            {
                if (c > 127)
                {
                    // This character is too big for ASCII
                    var e = "\\u" + ((int)c).ToString("x4");
                    b.Append(e);
                }
                else
                {
                    b.Append(c);
                }
            }
            return b.ToString();
        }

        public static bool StringContainsUnicode(string s) => s.Any(c => c > 255);

        public static string UnescapeString(string v)
        {
            if (v == null) return null;
            return Regex.Replace(
                v,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m => ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString());
        }

        public static string SerializeJsonWithEnumConverter(object d)
        {
            return JsonConvert.SerializeObject(
                d,
                Formatting.None,
                SharedSettings
            );
        }

        public static string PrettyPrintJson(string j)
        {
            var parsedJson = JsonConvert.DeserializeObject(
                j,
                SharedSettings
            );
            return JsonConvert.SerializeObject(
                parsedJson,
                Formatting.Indented,
                SharedSettings
            );
        }

        private static readonly JsonSerializerSettings SharedSettings = new()
        {
            Converters = new JsonConverter[]
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
    }
}
