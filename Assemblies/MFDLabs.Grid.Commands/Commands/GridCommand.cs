namespace MFDLabs.Grid.Commands;

/// <summary>
/// The base grid command for ScriptExecution on RCCService
/// </summary>
public abstract class GridCommand
{
    /// <summary>
    /// The command mode
    /// </summary>
    public abstract string Mode { get; }

    /// <summary>
    /// The version of the message
    /// </summary>
    public abstract int MessageVersion { get; }
}
