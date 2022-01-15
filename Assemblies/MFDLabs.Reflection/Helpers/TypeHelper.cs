using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MFDLabs.Reflection
{
    public static class TypeHelper
    {
        public static Type[] GetTypesInNamespace(Assembly assembly, string @namespace) =>
            assembly.GetTypes()
                .Where(t => string.Equals(t.Namespace, @namespace, StringComparison.Ordinal))
                .ToArray();

        public static void CopyValues<T>(T target, T source)
        {
            var t = typeof(T);

            var properties = t.GetProperties().Where(prop => prop.CanRead && prop.CanWrite);

            foreach (var prop in properties)
            {
                var value = prop.GetValue(source, null);
                if (value != null)
                    prop.SetValue(target, value, null);
            }
        }
        
        public static bool IsAnonymousType(Type type)
        {
            if (type == null) 
                throw new ArgumentNullException(nameof(type));

            if (Attribute.IsDefined(type,
                    typeof(CompilerGeneratedAttribute),
                    inherit: false) &&
                type.IsGenericType &&
                type.Name.Contains("AnonymousType") &&
                (type.Name.StartsWith("<>",
                     StringComparison.OrdinalIgnoreCase) ||
                 type.Name.StartsWith("VB$",
                     StringComparison.OrdinalIgnoreCase)))
            {
                // ReSharper disable once NonConstantEqualityExpressionHasConstantResult
                return (type.Attributes & TypeAttributes.NotPublic) == 0;
            }

            return false;
        }

        public static bool IsNumericType(Type o) =>
            Type.GetTypeCode(o) switch
            {
                TypeCode.Byte => true,
                TypeCode.SByte => true,
                TypeCode.UInt16 => true,
                TypeCode.UInt32 => true,
                TypeCode.UInt64 => true,
                TypeCode.Int16 => true,
                TypeCode.Int32 => true,
                TypeCode.Int64 => true,
                TypeCode.Decimal => true,
                TypeCode.Double => true,
                TypeCode.Single => true,
                TypeCode.Empty => false,
                TypeCode.Object => false,
                TypeCode.DBNull => false,
                TypeCode.Boolean => false,
                TypeCode.Char => false,
                TypeCode.DateTime => false,
                TypeCode.String => false,
                _ => false
            };

        public static bool IsPrimitive(Type type)
        {
            if (type.IsPrimitive) return true;

            return IsNumericType(type) || type == typeof(bool) || type == typeof(char);
        }
    }
}
