using System.Collections.Generic;
using System.Linq;
using System.IO;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Utility
{
    public static class LuaUtility
    {
        public static string SafeLuaMode
#if DEBUG
            => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Lua", "LuaVM.formatted.lua"));
#else
            => File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Lua", "LuaVM.lua"));
#endif


        public static string ParseLuaValues(IEnumerable<LuaValue> result) => Lua.ToString(result);

        public static bool CheckIfScriptContainsDisallowedText(string script, out string word)
        {
            word = null;

            var parsedScript = script;

            var escapedString = script.EscapeNewLines().Escape();

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase) 
                parsedScript = script.ToLower();

            SystemLogger.Singleton.Info("Check if script '{0}' contains blacklisted words.", escapedString);

            foreach (var keyword in GetBlacklistedKeywords())
            {
                var parsedKeyword = keyword;
                if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.ScriptExectionCareAboutBadTextCase) 
                    parsedKeyword = keyword.ToLower();
                
                if (!parsedScript.Contains(parsedKeyword)) continue;
                
                word = parsedKeyword;
                SystemLogger.Singleton.Warning("The script '{0}' contains blacklisted words.", escapedString);

                return true;
            }

            SystemLogger.Singleton.Info("The script '{0}' does not contain blacklisted words.", escapedString);
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
