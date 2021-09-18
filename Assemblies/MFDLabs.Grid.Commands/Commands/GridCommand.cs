namespace MFDLabs.Grid.Commands
{
    /// <summary>
    /// The base grid command for ScriptExecution on RCCService
    /// </summary>
    public abstract class GridCommand
    {
        public abstract string Mode { get; }

        public abstract int MessageVersion { get; }
    }
}