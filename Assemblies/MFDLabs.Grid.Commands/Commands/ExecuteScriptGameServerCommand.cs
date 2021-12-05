namespace MFDLabs.Grid.Commands
{
    public class ExecuteScriptGameServerCommand : GameServerCommand
    {
        public override string Mode => "ExecuteScript";
        public override int MessageVersion => 1;

        public ExecuteScriptGameServerCommand(ExecuteScriptGameServerSettings settings)
            : base(settings)
        {}
    }
}
