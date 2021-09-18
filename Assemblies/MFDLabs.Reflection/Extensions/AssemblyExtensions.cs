using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MFDLabs.Reflection.Extensions
{
    public static class AssemblyExtensions
    {
        public static Type[] GetTypesInAssembly(this Assembly assembly)
        {
            return assembly.GetTypes()
                     .ToArray();
        }

        public static Type[] GetTypesInAssemblyNamespace(this Assembly assembly, string @namespace) => TypeHelper.GetTypesInNamespace(assembly, @namespace);
    }
}
