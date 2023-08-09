namespace MFDLabs.Grid.Commands;

/// <summary>
/// The command to execute a script on the grid
/// </summary>
public class ExecuteScriptCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "ExecuteScript";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The settings for the command
    /// </summary>
    public ExecuteScriptSettings Settings { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ExecuteScriptCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public ExecuteScriptCommand(ExecuteScriptSettings settings) => Settings = settings;
}
