using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MFDLabs.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MFDLabs.Text
{
    public sealed class TextGlobal : SingletonBase<TextGlobal>
    {
        public enum StringDirection
        {
            Left,
            Right
        }

        public string FillString(string s, char c, int n, StringDirection d)
        {
            switch (d)
            {
                case StringDirection.Left:
                    s = new string(c, n) + s;
                    break;
                case StringDirection.Right:
                    s += new string(c, n);
                    break;
            }
            return s;
        }

        public string EscapeString(string s)
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

        public bool StringContainsUnicode(string s)
        {
            return s.Any(c => c > 255);
        }

        public string UnescapeString(string v)
        {
            if (v == null) return null;
            return Regex.Replace(
                v,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, NumberStyles.HexNumber)).ToString();
                }
            );
        }

        public string SerializeJsonWithEnumConverter(object d)
        {
            return JsonConvert.SerializeObject(
                d,
                Formatting.None,
                _SharedSettings
            );
        }

        public string PrettyPrintJson(string j)
        {
            var parsedJson = JsonConvert.DeserializeObject(
                j,
                _SharedSettings
            );
            return JsonConvert.SerializeObject(
                parsedJson,
                Formatting.Indented,
                _SharedSettings
            );
        }

        private static readonly JsonSerializerSettings _SharedSettings = new JsonSerializerSettings()
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
