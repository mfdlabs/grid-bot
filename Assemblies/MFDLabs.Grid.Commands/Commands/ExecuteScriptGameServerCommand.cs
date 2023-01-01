namespace MFDLabs.Grid.Commands;

/// <summary>
/// The command to execute scripts in game-server context.
/// </summary>
public class ExecuteScriptGameServerCommand : GameServerCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "ExecuteScript";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// Construct a new instance of <see cref="ExecuteScriptGameServerCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public ExecuteScriptGameServerCommand(ExecuteScriptGameServerSettings settings)
        : base(settings)
    {}
}
