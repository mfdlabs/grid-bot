namespace Grid.Commands;

/// <summary>
/// Evict a player from the grid
/// </summary>
public class EvictPlayerCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "EvictPlayer";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The settings for the command
    /// </summary>
    public EvictPlayerSettings Settings { get; }

    /// <summary>
    /// Construct a new instance of <see cref="EvictPlayerCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public EvictPlayerCommand(EvictPlayerSettings settings) => Settings = settings;
}
