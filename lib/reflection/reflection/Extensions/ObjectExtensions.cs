using System;

namespace Reflection.Extensions
{
    public static class ObjectExtensions
    {
        public static bool ToBoolean(this object obj) => Convert.ToBoolean(obj);
        public static bool ToBoolean(this object obj, IFormatProvider provider) => Convert.ToBoolean(obj, provider);
        public static byte ToByte(this object obj) => Convert.ToByte(obj);
        public static byte ToByte(this object obj, IFormatProvider provider) => Convert.ToByte(obj, provider);
        public static char ToChar(this object obj) => Convert.ToChar(obj);
        public static char ToChar(this object obj, IFormatProvider provider) => Convert.ToChar(obj, provider);
        public static DateTime ToDateTime(this object obj) => Convert.ToDateTime(obj);
        public static DateTime ToDateTime(this object obj, IFormatProvider provider) => Convert.ToDateTime(obj, provider);
        public static decimal ToDecimal(this object obj) => Convert.ToDecimal(obj);
        public static decimal ToDecimal(this object obj, IFormatProvider provider) => Convert.ToDecimal(obj, provider);
        public static double ToDouble(this object obj) => Convert.ToDouble(obj);
        public static double ToDouble(this object obj, IFormatProvider provider) => Convert.ToDouble(obj, provider);
        public static short ToInt16(this object obj) => Convert.ToInt16(obj);
        public static short ToInt16(this object obj, IFormatProvider provider) => Convert.ToInt16(obj, provider);
        public static int ToInt32(this object obj, IFormatProvider provider) => Convert.ToInt32(obj, provider);
        public static int ToInt32(this object obj) => Convert.ToInt32(obj);
        public static long ToInt64(this object obj, IFormatProvider provider) => Convert.ToInt64(obj, provider);
        public static long ToInt64(this object obj) => Convert.ToInt64(obj);
        public static sbyte ToSByte(this object obj, IFormatProvider provider) => Convert.ToSByte(obj, provider);
        public static sbyte ToSByte(this object obj) => Convert.ToSByte(obj);
        public static float ToSingle(this object obj, IFormatProvider provider) => Convert.ToSingle(obj, provider);
        public static float ToSingle(this object obj) => Convert.ToSingle(obj);
        public static ushort ToUInt16(this object obj, IFormatProvider provider) => Convert.ToUInt16(obj, provider);
        public static ushort ToUInt16(this object obj) => Convert.ToUInt16(obj);
        public static uint ToUInt32(this object obj, IFormatProvider provider) => Convert.ToUInt32(obj, provider);
        public static uint ToUInt32(this object obj) => Convert.ToUInt32(obj);
        public static ulong ToUInt64(this object obj, IFormatProvider provider) => Convert.ToUInt64(obj, provider);
        public static ulong ToUInt64(this object obj) => Convert.ToUInt64(obj);
        public static T OrDefault<T>(this T obj, T @default = default) => obj ?? @default;
    }
}
