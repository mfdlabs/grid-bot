using System;

#nullable enable

namespace MFDLabs.Reflection.Extensions
{
    public static class TypeExtensions
    {
        public static void Merge<T>(this T source, T target) => TypeHelper.CopyValues(target, source);
        public static bool IsNumeric(this Type t) => TypeHelper.IsNumericType(t);
        public static bool IsPrimitive(this Type t) => TypeHelper.IsPrimitive(t);
        public static bool IsAnonymous(this Type t) => TypeHelper.IsAnonymousType(t);

        public static TResult? To<TResult>(this object obj) => (TResult?)Convert.ChangeType(obj, typeof(TResult));
    }
}
