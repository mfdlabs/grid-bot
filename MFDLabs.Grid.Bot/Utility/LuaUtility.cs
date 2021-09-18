using MFDLabs.Abstractions;
using MFDLabs.Grid.ComputeCloud;
using MFDLabs.Logging;
using MFDLabs.Text;
using MFDLabs.Text.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace MFDLabs.Grid.Bot.Utility
{
    public sealed class LuaUtility : SingletonBase<LuaUtility>
    {
        public string ParseLuaValues(LuaValue[] result)
        {
            return Lua.ToString(result);
        }

        public bool CheckIfScriptContainsDisallowedText(string script, out string word)
        {
            word = null;

            var parsedScript = script;

            var escapedString = TextGlobal.Singleton.EscapeString(script.Replace("\n", "\\n"));

            if (!Settings.Singleton.ScriptExectionCareAboutBadTextCase) parsedScript = script.ToLower();

            SystemLogger.Singleton.Info("Check if script '{0}' contains blacklisted words.", escapedString);

            foreach (var keyword in GetBlacklistedKeywords())
            {
                var parsedKeyword = keyword;
                if (!Settings.Singleton.ScriptExectionCareAboutBadTextCase) parsedKeyword = keyword.ToLower();
                if (parsedScript.Contains(parsedKeyword))
                {
                    word = parsedKeyword;
                    SystemLogger.Singleton.Warning("The script '{0}' contains blacklisted words.", escapedString);

                    return true;
                }
            }

            SystemLogger.Singleton.Info("The script '{0}' does not contain blacklisted words.", escapedString);
            return false;
        }

        public IEnumerable<string> GetBlacklistedKeywords()
        {
            return from keyword in Settings.Singleton.BlacklistedScriptKeywords.Split(',') where !keyword.IsNullOrEmpty() select keyword;
        }
    }
}
