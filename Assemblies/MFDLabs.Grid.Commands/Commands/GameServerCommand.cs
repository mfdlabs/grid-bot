namespace MFDLabs.Grid.Commands
{
    public class GameServerCommand : GridCommand
    {
        public override string Mode => "GameServer";
        public override int MessageVersion => 1;
        public GameServerSettings Settings { get; }

        public GameServerCommand(GameServerSettings settings) => Settings = settings;
    }
}