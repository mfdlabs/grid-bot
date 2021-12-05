namespace MFDLabs.Grid.Commands
{
    public class EvictPlayerCommand : GridCommand
    {
        public override string Mode => "EvictPlayer";
        public override int MessageVersion => 1;
        public EvictPlayerSettings Settings { get; }

        public EvictPlayerCommand(EvictPlayerSettings settings) => Settings = settings;
    }
}