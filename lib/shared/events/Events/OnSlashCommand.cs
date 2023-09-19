#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.Events;

using System.Threading.Tasks;

using Discord.WebSocket;

using WorkQueues;

/// <summary>
/// Event handler for the <see cref="OnSlashCommandReceivedWorkQueue"/>
/// </summary>
public static class OnSlashCommand
{
    /// <summary>
    /// Invoke the event handler.
    /// </summary>
    /// <param name="command">The <see cref="SocketSlashCommand"/></param>
    public static Task Invoke(SocketSlashCommand command)
    {
        OnSlashCommandReceivedWorkQueue.Singleton.EnqueueWorkItem(command);

        return Task.CompletedTask;
    }
}

#endif // WE_LOVE_EM_SLASH_COMMANDS
