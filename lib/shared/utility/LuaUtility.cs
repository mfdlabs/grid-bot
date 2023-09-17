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
public static class LuaUtility
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

    /// <summary>
    /// Represents the metadata returned by the lua-vm script.
    /// </summary>
    public struct ReturnMetadata
    {
        /// <summary>
        /// Is the script a success?
        /// </summary>
        [JsonProperty("success")]
        public bool Success;

        /// <summary>
        /// The total execution time.
        /// </summary>
        [JsonProperty("execution_time")]
        public double ExecutionTime;

        /// <summary>
        /// The optional error message.
        /// </summary>
        [JsonProperty("error_message")]
        public string ErrorMessage;

        /// <summary>
        /// The optional logs.
        /// </summary>
        [JsonProperty("logs")]
        public string Logs;
    }

    /// <summary>
    /// The template file for the lua-vm.
    /// </summary>
    public static string LuaVMTemplate
        => FixFormatString(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "lua", "lua-vm.lua")));


    /// <summary>
    /// Parse the return metadata from the grid-server.
    /// </summary>
    /// <param name="result">The <see cref="LuaValue"/>s</param>
    /// <returns>The result and the <see cref="ReturnMetadata"/></returns>
    public static (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result)
    {
        return (
            (string)Lua.ConvertLua(result.FirstOrDefault()), 
            JsonConvert.DeserializeObject<ReturnMetadata>((string)Lua.ConvertLua(result.ElementAtOrDefault(1)))
        );
    }
}
