using Discord.WebSocket;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnBotGlobalAddedToGuild
    {
        public static Task Invoke(SocketGuild guild) => Task.CompletedTask;
    }
}
