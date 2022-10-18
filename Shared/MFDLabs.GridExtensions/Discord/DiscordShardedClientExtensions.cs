#if WE_LOVE_EM_SLASH_COMMANDS && DISCORD_SHARDING_ENABLED

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Threading.Extensions;

// Use the first shard in order to do any gateway actions.
// TODO: Load balance these?

namespace MFDLabs.Grid.Bot.Extensions
{
    public static class DiscordShardedClientExtensions
    {
        public static SocketApplicationCommand CreateGlobalApplicationCommand(
            this DiscordShardedClient client,
            ApplicationCommandProperties properties,
            RequestOptions options = null
        ) 
            => client.GetShard(0).CreateGlobalApplicationCommandAsync(properties, options).Sync();

        public static ValueTask<SocketApplicationCommand> GetGlobalApplicationCommandAsync(this DiscordShardedClient client, ulong id, RequestOptions requestOptions = null)
            => client.GetShard(0).GetGlobalApplicationCommandAsync(id, requestOptions);

        public static ValueTask<IUser> GetUserAsync(this DiscordShardedClient client, ulong userId, RequestOptions requestOptions = null)
            => client.GetShard(0).GetUserAsync(userId, requestOptions);
    }
}

#endif