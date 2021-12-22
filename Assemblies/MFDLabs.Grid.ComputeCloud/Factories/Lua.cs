using System;
using System.Collections.Generic;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.ComputeCloud
{
    public static class Lua
    {
        public static ScriptExecution NewScript(string name, string script) => NewScript(name, script, NewArgs(new object[] { }));
        public static ScriptExecution NewScript(string name, string script, params object[] args) => NewScript(name, script, NewArgs(args));
        public static ScriptExecution NewScript(string name, string script, LuaValue[] args) 
            => new ScriptExecution
            {
                name = name,
                script = script,
                arguments = args ?? NewArgs(new object[] { })
            };
        public static string ToString(IEnumerable<LuaValue> result)
        {
            string data = null;
            foreach (var luaValue in result)
            {
                var value = luaValue.value;
                if (luaValue.table != null) 
                    value = "[" + ToString(luaValue.table) + "]";
                if (data.IsNullOrEmpty())
                    data = value; 
                else
                    data = data + ", " + value;
            }
            return data;
        }
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
                    if (!(value is LuaValue[] values)) throw new ArgumentException($"Unsupported Lua argument type {value.GetType()}, value = '{value}'");
                    luaValue.type = LuaType.LUA_TTABLE;
                    luaValue.table = values;
                    break;
                }
            }
            args[index] = luaValue;
        }
        private static object ConvertLua(LuaValue luaValue)
        {
            switch (luaValue.type)
            {
                case LuaType.LUA_TBOOLEAN:
                    return Convert.ToBoolean(luaValue.value);
                case LuaType.LUA_TNUMBER:
                    return Convert.ToDouble(luaValue.value);
                case LuaType.LUA_TSTRING:
                    return luaValue.value;
                case LuaType.LUA_TTABLE:
                    return GetValues(luaValue.table);
                case LuaType.LUA_TNIL:
                default:
                    return null;
            }
        }
        public static LuaValue[] NewArgs(params object[] args)
        {
            var luaValues = new LuaValue[args.Length];
            for (int i = 0; i < args.Length; i++) SetArg(luaValues, i, args[i]);
            return luaValues;
        }
        public static object[] GetValues(LuaValue[] args)
        {
            var values = new object[args.Length];
            for (var i = 0; i < args.Length; i++) values[i] = ConvertLua(args[i]);
            return values;
        }

        public static readonly ScriptExecution EmptyScript = NewScript("EmptyScript", "return");
    }
}
