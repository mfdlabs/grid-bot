namespace MFDLabs.Grid.Commands
{
    public class ThumbnailCommand : GridCommand
    {
        public override string Mode { get; }
        public override int MessageVersion => 1;
        public ThumbnailSettings Settings { get; }

        public ThumbnailCommand(ThumbnailSettings settings)
        {
            Settings = settings;
            Mode = (Settings.Type == ThumbnailCommandType.TexturePack) ? "ExecuteScript" : "Thumbnail";
        }
    }
}