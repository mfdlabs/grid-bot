/* Copyright MFDLABS Corporation. All rights reserved. */

using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Models
{
    public sealed class SocketTaskRequest
    {
        public SocketMessage Message { get; set; }
        public string[] ContentArray { get; set; }
        public string OriginalCommandName { get; set; }
    }
}
