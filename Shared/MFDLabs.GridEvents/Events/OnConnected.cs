/* Copyright MFDLABS Corporation. All rights reserved. */

using System.Threading.Tasks;
using MFDLabs.Logging;

#if DISCORD_SHARDING_ENABLED
using Discord.WebSocket;
#endif

namespace MFDLabs.Grid.Bot.Events
{

#if DISCORD_SHARDING_ENABLED
    public static class OnShardConnected
    {
        public static Task Invoke(DiscordSocketClient shard)
        {
            SystemLogger.Singleton.Debug("Shard '{0}' has been connected to the Hub.", shard.ShardId);
            return Task.CompletedTask;
        }
    }
#else
    public static class OnConnected
    {
        public static Task Invoke()
        {
            SystemLogger.Singleton.Debug("Client has been connected to the Hub.");
            return Task.CompletedTask;
        }
    }
#endif

}
