using Discord.WebSocket;
using MFDLabs.Concurrency.Base.Async;

namespace MFDLabs.Grid.Bot.Base
{
    public abstract class HoldsSocketMessagePortExpiringTaskThread<TSingleton> : AsyncExpiringTaskThread<TSingleton, SocketMessage>
        where TSingleton : AsyncExpiringTaskThread<TSingleton, SocketMessage>, new()
    {
    }
}
