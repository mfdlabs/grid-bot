namespace Grid;

using System;
using System.Collections.Generic;

using ComputeCloud;

/// <summary>
/// <see cref="LuaValue"/> helper methods.
/// </summary>
public static class Lua
{
    /// <summary>
    /// The default empty script instance.
    /// </summary>
    public static readonly ScriptExecution EmptyScript = NewScript("EmptyScript", "return");

    /// <summary>
    /// Create a <see cref="ScriptExecution"/> with the specified <paramref name="script"/> and <paramref name="name"/>.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <param name="script">The script to execute.</param>
    /// <returns>A <see cref="ScriptExecution"/> instance.</returns>
    public static ScriptExecution NewScript(string name, string script) 
        => NewScript(name, script, NewArgs(Array.Empty<object>()));

    /// <summary>
    /// Create a <see cref="ScriptExecution"/> with the specified <paramref name="script"/> and <paramref name="name"/>.
    /// 
    /// With arguments.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>A <see cref="ScriptExecution"/> instance.</returns>
    public static ScriptExecution NewScript(string name, string script, params object[] args) 
        => NewScript(name, script, NewArgs(args));

    /// <summary>
    /// Create a <see cref="ScriptExecution"/> with the specified <paramref name="script"/> and <paramref name="name"/>.
    /// 
    /// With <see cref="LuaValue"/> arguments.
    /// </summary>
    /// <param name="name">The name of the script.</param>
    /// <param name="script">The script to execute.</param>
    /// <param name="args">The arguments.</param>
    /// <returns>A <see cref="ScriptExecution"/> instance.</returns>
    public static ScriptExecution NewScript(string name, string script, params LuaValue[] args) 
        => new()
        {
            name = name,
            script = script,
            arguments = args ?? NewArgs(Array.Empty<object>())
        };

    /// <summary>
    /// Convert the resulting <see cref="LuaValue"/> array to string.
    /// </summary>
    /// <param name="result">The <see cref="LuaValue"/> array.</param>
    /// <returns>The string result.</returns>
    public static string ToString(IEnumerable<LuaValue> result)
    {
        string data = null;
        foreach (var luaValue in result)
        {
            var value = luaValue.value;

            if (luaValue.table != null) 
                value = "[" + ToString(luaValue.table) + "]";

            if (string.IsNullOrEmpty(data))
                data = value; 
            else
                data = data + ", " + value;
        }

        return data;
    }

    /// <summary>
    /// Set an argument at the specified <paramref name="index"/> in the <see cref="LuaValue"/> args.
    /// </summary>
    /// <param name="args">The <see cref="LuaValue"/> args.</param>
    /// <param name="index">The index to set.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentException">Unsupported Lua argument type.</exception>
    public static void SetArg(LuaValue[] args, int index, object value)
    {
        var luaValue = new LuaValue();
        switch (value)
        {
            case int _:
            case float _:
            case double _:
            case long _:
            case decimal _:
            case short _:
            case ushort _:
            case uint _:
            case ulong _:
                luaValue.type = LuaType.LUA_TNUMBER;
                luaValue.value = value.ToString();

                break;
            case string _:
            case Guid _:
                luaValue.type = LuaType.LUA_TSTRING;
                luaValue.value = value.ToString();

                break;
            case bool boolean:
                luaValue.type = LuaType.LUA_TBOOLEAN;
                luaValue.value = boolean ? "true" : "false";

                break;
            case null:
                luaValue.type = LuaType.LUA_TNIL;
                luaValue.value = string.Empty;

                break;
            default:
            {
                if (value is not LuaValue[] values) 
                    throw new ArgumentException($"Unsupported Lua argument type {value.GetType()}, value = '{value}'");

                luaValue.type = LuaType.LUA_TTABLE;
                luaValue.table = values;

                break;
            }
        }

        args[index] = luaValue;
    }

    /// <summary>
    /// Convert a <see cref="LuaValue"/> to it's actual value.
    /// </summary>
    /// <param name="luaValue">The <see cref="LuaValue"/></param>
    /// <returns>The actual value of the <see cref="LuaValue"/></returns>
    private static object ConvertLua(LuaValue luaValue) 
        => luaValue.type switch
        {
            LuaType.LUA_TBOOLEAN => Convert.ToBoolean(luaValue.value),
            LuaType.LUA_TNUMBER => Convert.ToDouble(luaValue.value),
            LuaType.LUA_TSTRING => luaValue.value,
            LuaType.LUA_TTABLE => GetValues(luaValue.table),
            _ => null,
        };

    /// <summary>
    /// Create a new <see cref="LuaValue"/> argument list.
    /// </summary>
    /// <param name="args">The raw arguments.</param>
    /// <returns>The <see cref="LuaValue"/> argument list.</returns>
    public static LuaValue[] NewArgs(params object[] args)
    {
        var luaValues = new LuaValue[args.Length];

        for (int i = 0; i < args.Length; i++) 
            SetArg(luaValues, i, args[i]);

        return luaValues;
    }

    /// <summary>
    /// Get the raw values from a <see cref="LuaValue"/> argument list.
    /// </summary>
    /// <param name="args">The <see cref="LuaValue"/> arguments.</param>
    /// <returns>The  raw values.</returns>
    public static object[] GetValues(LuaValue[] args)
    {
        var values = new object[args.Length];
        for (var i = 0; i < args.Length; i++) values[i] = ConvertLua(args[i]);
        return values;
    }
}
