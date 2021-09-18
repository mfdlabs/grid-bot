namespace MFDLabs.Grid.Commands
{
    public class ExecuteScriptCommand : GridCommand
    {
        public override string Mode => "ExecuteScript";

        public override int MessageVersion => 1;

        public ExecuteScriptSettings Settings { get; }

        public ExecuteScriptCommand(ExecuteScriptSettings settings)
        {
            Settings = settings;
        }
    }
}
