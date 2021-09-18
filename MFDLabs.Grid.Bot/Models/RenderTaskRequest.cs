using Discord.WebSocket;
using System;

namespace MFDLabs.Grid.Bot.Models
{
    [Serializable]
    internal sealed class RenderTaskRequest
    {
        public SocketMessage Message { get; set; }
        public string[] ContentArray { get; set; }
        public string OriginalCommandName { get; set; }
    }
}
