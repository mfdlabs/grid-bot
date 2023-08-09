using System.Threading.Tasks;
using Discord.WebSocket;
using Grid.Bot.WorkQueues;

namespace Grid.Bot.Events
{
    public static class OnMessage
    {
        public static Task Invoke(SocketMessage message)
        {
            OnMessageReceivedWorkQueue.Singleton.EnqueueWorkItem(message);
            return Task.CompletedTask;
        }
    }
}
