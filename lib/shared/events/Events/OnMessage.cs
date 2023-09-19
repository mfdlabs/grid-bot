namespace Grid.Bot.Events;

using System.Threading.Tasks;

using Discord.WebSocket;

using WorkQueues;

/// <summary>
/// Event handler for the <see cref="OnMessageReceivedWorkQueue"/>
/// </summary>
public static class OnMessage
{
    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="message">The <see cref="SocketMessage"/></param>
    public static Task Invoke(SocketMessage message)
    {
        OnMessageReceivedWorkQueue.Singleton.EnqueueWorkItem(message);

        return Task.CompletedTask;
    }
}
