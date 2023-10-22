namespace Grid.Commands;

/// <summary>
/// The command for issuing an action on the grid
/// </summary>
public class ServerActionCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "ServerAction";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The settings for the command
    /// </summary>
    public ServerActionSettings Settings { get; }

    /// <summary>
    /// Construct a new instance of <see cref="ServerActionCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public ServerActionCommand(ServerActionSettings settings) => Settings = settings;
}
