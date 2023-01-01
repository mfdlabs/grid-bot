namespace MFDLabs.Grid.Commands;

using System.Collections.Generic;

/// <summary>
/// Settings for <see cref="ExecuteScriptCommand"/>
/// </summary>
public class ExecuteScriptSettings
{
    /// <summary>
    /// The type of script/name of the script.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// The arguments to pass to the script.
    /// </summary>
    public IDictionary<string, object> Arguments { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ExecuteScriptSettings"/>
    /// </summary>
    /// <param name="type">The type of script/name of the script.</param>
    /// <param name="arguments">The arguments to pass to the script.</param>
    public ExecuteScriptSettings(string type, IDictionary<string, object> arguments)
    {
        Type = type;
        Arguments = arguments;
    }
}
