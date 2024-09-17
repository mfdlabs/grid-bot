namespace Grid.Bot.Utility;

using System.IO;
using System.Linq;
using System.Text;
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
    private static readonly Assembly _assembly = Assembly.GetExecutingAssembly();
    private const string _luaVmResource = "Grid.Bot.Lua.LuaVMTemplate.lua";

    [GeneratedRegex(@"{{(\d{1,2})}}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FormatPartRegex();


    private static readonly string _LuaVM;

    static LuaUtility()
    {
        using var stream = _assembly.GetManifestResourceStream(_luaVmResource);
        using var reader = new StreamReader(stream);

        _LuaVM = FixFormatString(reader.ReadToEnd());
    }

    private static string FixFormatString(string input)
    {
        input = input.Replace("{", "{{");
        input = input.Replace("}", "}}");

        input = FormatPartRegex().Replace(input, (m) => { return $"{{{m.Groups[1]}}}"; });

        return input;
    }

    /// <inheritdoc cref="ILuaUtility.LuaVMTemplate"/>
    public string LuaVMTemplate => _LuaVM;

    /// <inheritdoc cref="ILuaUtility.ParseResult(IEnumerable{LuaValue})"/>
    public (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result)
    {
        if (result.Count() == 1)
        {
            // Legacy case, where LuaVM is not enabled.

            var mockMetadata = new ReturnMetadata();
            mockMetadata.Success = true;

            return ((string)Lua.ConvertLua(result.First()), mockMetadata);
        }

        return (
            (string)Lua.ConvertLua(result.FirstOrDefault()),
            JsonConvert.DeserializeObject<ReturnMetadata>((string)Lua.ConvertLua(result.ElementAtOrDefault(1)))
        );
    }
}
