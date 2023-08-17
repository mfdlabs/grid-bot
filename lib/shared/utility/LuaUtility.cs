﻿using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Logging;

using Text.Extensions;
using Grid.ComputeCloud;

namespace Grid.Bot.Utility
{
    public static class LuaUtility
    {
        public struct ReturnMetadata
        {
            [JsonProperty("success")]
            public bool Success;

            [JsonProperty("execution_time")]
            public double ExecutionTime;

            [JsonProperty("error_message")]
            public string ErrorMessage;

            [JsonProperty("logs")]
            public string Logs;
        }

        public static string SafeLuaMode
            => FixFormatString(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "lua", "lua-vm.lua")));


        private static string FixFormatString(string input)
        {
            //language=regex
            const string partRegex = @"{{(\d{1,2})}}";

            input = input.Replace("{", "{{");
            input = input.Replace("}", "}}");

            input = Regex.Replace(input, partRegex, (m) => { return $"{{{m.Groups[1]}}}"; });

            return input;
        }

        public static string ParseLuaValues(IEnumerable<LuaValue> result) => Lua.ToString(result);

        public static (string result, ReturnMetadata metadata) ParseResult(IEnumerable<LuaValue> result)
        {
            return (
                (string)Lua.ConvertLua(result.FirstOrDefault()), 
                JsonConvert.DeserializeObject<ReturnMetadata>((string)Lua.ConvertLua(result.ElementAtOrDefault(1)))
            );
        }

        public static bool CheckIfScriptContainsDisallowedText(string script, out string word)
        {
            word = null;

            var parsedScript = script;

            var escapedString = script.EscapeNewLines().Escape();

            if (!global::Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase) 
                parsedScript = script.ToLower();

            Logger.Singleton.Information("Check if script '{0}' contains blacklisted words.", escapedString);

            foreach (var keyword in GetBlacklistedKeywords())
            {
                var parsedKeyword = keyword;
                if (!global::Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase) 
                    parsedKeyword = keyword.ToLower();
                
                if (!parsedScript.Contains(parsedKeyword)) continue;
                
                word = parsedKeyword;
                Logger.Singleton.Warning("The script '{0}' contains blacklisted words.", escapedString);

                return true;
            }

            Logger.Singleton.Information("The script '{0}' does not contain blacklisted words.", escapedString);
            return false;
        }

        private static IEnumerable<string> GetBlacklistedKeywords() 
            =>
                (from keyword in global::Grid.Bot.Properties.Settings.Default.BlacklistedScriptKeywords
                        .Split(',')
                    where !keyword.IsNullOrEmpty()
                    select keyword);
    }
}
