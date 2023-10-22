namespace Grid.Bot.Utility;

using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Grid.ComputeCloud;

/// <summary>
/// Utility for interacting with grid-server Lua.
/// </summary>
public class LuaUtility : ILuaUtility
{
    private static string FixFormatString(string input)
    {
        //language=regex
        const string partRegex = @"{{(\d{1,2})}}";

        input = input.Replace("{", "{{");
        input = input.Replace("}", "}}");

        input = Regex.Replace(input, partRegex, (m) => { return $"{{{m.Groups[1]}}}"; });

        return input;
    }

    /// <inheritdoc cref="ILuaUtility.LuaVMTemplate"/>
    public string LuaVMTemplate
        => FixFormatString(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "lua", "lua-vm.lua")));

    /// <inheritdoc cref="ILuaUtility.ParseResult(IEnumerable{LuaValue})"/>
    public (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result)
    {
        return (
            (string)Lua.ConvertLua(result.FirstOrDefault()), 
            JsonConvert.DeserializeObject<ReturnMetadata>((string)Lua.ConvertLua(result.ElementAtOrDefault(1)))
        );
    }
}
