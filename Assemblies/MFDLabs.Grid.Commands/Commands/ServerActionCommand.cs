namespace MFDLabs.Grid.Commands
{
    public class ServerActionCommand : GridCommand
    {
        public override string Mode => "ServerAction";
        public override int MessageVersion => 1;
        public ServerActionSettings Settings { get; }

        public ServerActionCommand(ServerActionSettings settings) => Settings = settings;
    }
}