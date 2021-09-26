using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Models
{
    internal sealed class RenderTaskRequest
    {
        public SocketMessage Message { get; set; }
        public string[] ContentArray { get; set; }
        public string OriginalCommandName { get; set; }
    }
}
