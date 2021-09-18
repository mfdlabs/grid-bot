using System;

namespace MFDLabs.Reflection.Extensions
{
    public static class TypeExtensions
    {
        public static void Merge<T>(this T source, T target) => TypeHelper.CopyValues(target, source);
        public static bool IsNumeric(this Type t) => TypeHelper.IsNumericType(t);
        public static bool IsPrimitave(this Type t) => TypeHelper.IsPrimitive(t);
    }
}
