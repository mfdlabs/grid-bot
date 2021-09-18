using MFDLabs.Text.Extensions;
using System;

namespace MFDLabs.Grid.ComputeCloud
{
    public static class Lua
    {
        public static ScriptExecution NewScript(string name, string script)
        {
            return NewScript(name, script, NewArgs(new object[] { }));
        }

        public static ScriptExecution NewScript(string name, string script, params object[] args)
        {
            return NewScript(name, script, NewArgs(args));
        }

        public static ScriptExecution NewScript(string name, string script, LuaValue[] args)
        {
            return new ScriptExecution
            {
                name = name,
                script = script,
                arguments = args ?? NewArgs(new object[] { })
            };
        }

        public static string ToString(LuaValue[] result)
        {
            string data = null;
            foreach (var luaValue in result)
            {
                string value = luaValue.value;
                if (luaValue.table != null)
                {
                    value = "[" + ToString(luaValue.table) + "]";
                }
                if (data.IsNullOrEmpty())
                {
                    data = value;
                }
                else
                {
                    data = data + ", " + value;
                }
            }
            return data;
        }

        public static void SetArg(LuaValue[] args, int index, object value)
        {
            var luaValue = new LuaValue();
            if (value is int || value is float || value is double || value is long || value is decimal || value is short || value is ushort || value is uint || value is ulong)
            {
                luaValue.type = LuaType.LUA_TNUMBER;
                luaValue.value = value.ToString();
            }
            else if (value is string || value is Guid)
            {
                luaValue.type = LuaType.LUA_TSTRING;
                luaValue.value = value.ToString();
            }
            else if (value is bool boolean)
            {
                luaValue.type = LuaType.LUA_TBOOLEAN;
                luaValue.value = boolean ? "true" : "false";
            }
            else if (value == null)
            {
                luaValue.type = LuaType.LUA_TNIL;
                luaValue.value = string.Empty;
            }
            else
            {
                if (!(value is LuaValue[]))
                {
                    throw new ArgumentException($"Unsupported Lua argument type {value.GetType()}, value = '{value}'");
                }
                luaValue.type = LuaType.LUA_TTABLE;
                luaValue.table = value as LuaValue[];
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
                default:
                    return null;
            }
        }

        public static LuaValue[] NewArgs(params object[] args)
        {
            var luaValues = new LuaValue[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                SetArg(luaValues, i, args[i]);
            }
            return luaValues;
        }

        public static object[] GetValues(LuaValue[] args)
        {
            var values = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                values[i] = ConvertLua(args[i]);
            }
            return values;
        }

        public static readonly ScriptExecution EmptyScript = NewScript("EmptyScript", "return");
    }
}
