#r "C:\git\MFDLabs\MFDLabs.Grid\MFDLabs.Grid.Bot\bin\Debug\MFDLabs.Grid.Bot.exe"

using MFDLabs.Grid.ComputeCloud;
using System.Reflection;
using System;
using System.Collections.Generic;

var properties = typeof(Lua).GetProperties(BindingFlags.Public | BindingFlags.Static);
var methods = typeof(Lua).GetMethods(BindingFlags.Public | BindingFlags.Static);
var fields = typeof(Lua).GetFields(BindingFlags.Public | BindingFlags.Static);

foreach (var m in methods)
{
    try
    {
        string name = m.Name;
        var values = new List<string>();

        foreach (var p in m.GetParameters())
        {

            values.Add($"{p.ParameterType.Name} {p.Name}");
        }

        Console.WriteLine("{0} {1}.{2}({3})", m.ReturnType.FullName, m.DeclaringType.FullName, name, string.Join(", ", values));
    }
    catch { }
}

foreach (var p in properties)
{
    try
    {
        string name = p.Name;
        var value = p.GetValue(null, null);
        Console.WriteLine("{0}: {1}", name, value);
    }
    catch { }
}

foreach (var f in fields)
{
    try
    {
        string name = f.Name;
        var value = f.GetValue(null);
        Console.WriteLine("{0}: {1}", name, value);
    }
    catch { }
}
