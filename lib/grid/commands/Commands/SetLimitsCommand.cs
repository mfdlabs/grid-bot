namespace Grid.Commands;

/// <summary>
/// The command for setting the limits on the grid
/// </summary>
public class SetLimitsCommand : GridCommand
{
    /// <inheritdoc cref="GridCommand.Mode"/>
    public override string Mode => "SetLimits";

    /// <inheritdoc cref="GridCommand.MessageVersion"/>
    public override int MessageVersion => 1;

    /// <summary>
    /// The limits.
    /// </summary>
    public SetLimitsSettings Settings { get; }

    /// <summary>
    /// Construct a new instanc of <see cref="SetLimitsCommand"/>
    /// </summary>
    /// <param name="settings">The settings for the command</param>
    public SetLimitsCommand(SetLimitsSettings settings) => Settings = settings;
}
