using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using MFDLabs.Grid.ComputeCloud;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class LuaUtility
    {
        public static string SafeLuaMode => FixFormatString(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Lua", "LuaVM.lua")));

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

        public static bool CheckIfScriptContainsDisallowedText(string script, out string word)
        {
            word = null;

            var parsedScript = script;

            var escapedString = script.EscapeNewLines().Escape();

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase) 
                parsedScript = script.ToLower();

            Logger.Singleton.Info("Check if script '{0}' contains blacklisted words.", escapedString);

            foreach (var keyword in GetBlacklistedKeywords())
            {
                var parsedKeyword = keyword;
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase) 
                    parsedKeyword = keyword.ToLower();
                
                if (!parsedScript.Contains(parsedKeyword)) continue;
                
                word = parsedKeyword;
                Logger.Singleton.Warning("The script '{0}' contains blacklisted words.", escapedString);

                return true;
            }

            Logger.Singleton.Info("The script '{0}' does not contain blacklisted words.", escapedString);
            return false;
        }

        private static IEnumerable<string> GetBlacklistedKeywords() 
            =>
                (from keyword in global::MFDLabs.Grid.Bot.Properties.Settings.Default.BlacklistedScriptKeywords
                        .Split(',')
                    where !keyword.IsNullOrEmpty()
                    select keyword);
    }
}
