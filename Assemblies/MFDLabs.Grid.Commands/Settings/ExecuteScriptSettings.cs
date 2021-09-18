using System.Collections.Generic;

namespace MFDLabs.Grid.Commands
{
    public class ExecuteScriptSettings
    {
        public string Type { get; }

        public IDictionary<string, object> Arguments { get; }

        public ExecuteScriptSettings(string type, IDictionary<string, object> arguments)
        {
            Type = type;
            Arguments = arguments;
        }
    }
}
