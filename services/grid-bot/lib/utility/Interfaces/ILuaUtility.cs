namespace Grid.Bot.Utility;

using System.Collections.Generic;

using Newtonsoft.Json;

using Client;

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
/// Utility class for managing Lua scripts.
/// </summary>
public interface ILuaUtility
{
    /// <summary>
    /// The template file for the lua-vm.
    /// </summary>
    string LuaVmTemplate { get; }

    /// <summary>
    /// Parse the return metadata from the grid-server.
    /// </summary>
    /// <param name="result">The <see cref="LuaValue"/>s</param>
    /// <returns>The result and the <see cref="ReturnMetadata"/></returns>
   (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result);
}
