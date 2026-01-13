using System;

namespace Grid.Bot.Utility;

using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Client;

/// <summary>
/// Utility for interacting with grid-server Lua.
/// </summary>
public partial class LuaUtility : ILuaUtility
{
    private static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
    private const string LuaVmResource = "Grid.Bot.Lua.LuaVMTemplate.lua";

    [GeneratedRegex(@"{{(\d{1,2})}}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FormatPartRegex();


    private static readonly string LuaVm;

    static LuaUtility()
    {
        using var stream = Assembly.GetManifestResourceStream(LuaVmResource);
        using var reader = new StreamReader(stream ?? throw new InvalidOperationException());

        LuaVm = FixFormatString(reader.ReadToEnd());
    }

    private static string FixFormatString(string input)
    {
        input = input.Replace("{", "{{");
        input = input.Replace("}", "}}");

        input = FormatPartRegex().Replace(input, (m) => $"{{{m.Groups[1]}}}");

        return input;
    }

    /// <inheritdoc cref="ILuaUtility.LuaVmTemplate"/>
    public string LuaVmTemplate => LuaVm;

    /// <inheritdoc cref="ILuaUtility.ParseResult(IEnumerable{LuaValue})"/>
    public (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result)
    {
        var luaValues = result as LuaValue[] ?? result.ToArray();
        
        if (luaValues.Length != 1)
            return (
                (string)Lua.ConvertLua(luaValues.FirstOrDefault()),
                JsonConvert.DeserializeObject<ReturnMetadata>((string)Lua.ConvertLua(luaValues.ElementAtOrDefault(1)))
            );
        
        // Legacy case, where LuaVM is not enabled.

        var mockMetadata = new ReturnMetadata
        {
            Success = true
        };

        return ((string)Lua.ConvertLua(luaValues.First()), mockMetadata);
    }
}
