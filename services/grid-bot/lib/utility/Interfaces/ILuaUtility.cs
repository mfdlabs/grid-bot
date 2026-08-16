namespace Grid.Bot.Utility;

using System.Collections.Generic;

using Client;

/// <summary>
/// Utility class for managing Lua scripts.
/// </summary>
public interface ILuaUtility
{
    /// <summary>
    /// The template file for the lua-vm.
    /// </summary>
    string LuaVMTemplate { get; }

    /// <summary>
    /// Parse the return metadata from the grid-server.
    /// </summary>
    /// <param name="result">The <see cref="LuaValue"/>s</param>
    /// <returns>The result and the <see cref="ReturnMetadata"/></returns>
   (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result);
}
