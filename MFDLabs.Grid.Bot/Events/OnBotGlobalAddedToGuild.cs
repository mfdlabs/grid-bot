using System.Threading.Tasks;
using Discord.WebSocket;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnBotGlobalAddedToGuild
    {
        public static Task Invoke(SocketGuild guild) => Task.CompletedTask;
    }
}
