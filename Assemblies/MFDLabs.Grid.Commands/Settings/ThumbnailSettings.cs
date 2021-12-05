namespace MFDLabs.Grid.Commands
{
    public class ThumbnailSettings
    {
        public ThumbnailCommandType Type { get; }
        public object[] Arguments { get; }

        public ThumbnailSettings(ThumbnailCommandType type, params object[] arguments)
        {
            Type = type;
            Arguments = arguments;
        }
    }
}