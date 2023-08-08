#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.WorkQueues;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnSlashCommand
    {
        public static Task Invoke(SocketSlashCommand command)
        {
            OnSlashCommandReceivedWorkQueue.Singleton.EnqueueWorkItem(command);
            return Task.CompletedTask;
        }
    }
}

#endif // WE_LOVE_EM_SLASH_COMMANDS