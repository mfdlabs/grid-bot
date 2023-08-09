namespace MFDLabs.Grid.Commands;

/// <summary>
/// The command to launch a game server on the grid
/// </summary>
public class GameServerCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "GameServer";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The settings for the command
    /// </summary>
    public GameServerSettings Settings { get; }

    /// <summary>
    /// Construct a new instance of <see cref="GameServerCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public GameServerCommand(GameServerSettings settings) => Settings = settings;
}
