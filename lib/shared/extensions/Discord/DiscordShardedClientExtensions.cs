#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.Extensions;

using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using Random;
using Threading.Extensions;

/// <summary>
/// Extension methods for the <see cref="DiscordShardedClient"/>
/// </summary>
public static class DiscordShardedClientExtensions
{
    private static readonly IRandom _random = RandomFactory.GetDefaultRandom();

    /// <summary>
    /// Gets a random shard from the <see cref="DiscordShardedClient"/>
    /// </summary>
    /// <param name="client">The <see cref="DiscordShardedClient"/></param>
    /// <returns>The <see cref="DiscordSocketClient"/></returns>
    public static DiscordSocketClient GetShard(this DiscordShardedClient client)
        => client.GetShard(_random.Next(0, client.Shards.Count - 1));

    /// <inheritdoc cref="DiscordSocketClient.CreateGlobalApplicationCommandAsync(ApplicationCommandProperties, RequestOptions)"/>
    public static SocketApplicationCommand CreateGlobalApplicationCommand(
        this DiscordShardedClient client,
        ApplicationCommandProperties properties,
        RequestOptions options = null
    )
        => client.GetShard().CreateGlobalApplicationCommandAsync(properties, options).Sync();

    /// <inheritdoc cref="DiscordSocketClient.GetGlobalApplicationCommandsAsync(bool, string, RequestOptions)"/>
    public static IReadOnlyCollection<SocketApplicationCommand> GetGlobalApplicationCommands(
        this DiscordShardedClient client,
        bool withLocalizations = false,
        string locale = null,
        RequestOptions options = null
    )
        => client.GetShard().GetGlobalApplicationCommandsAsync(withLocalizations, locale, options).Sync();

    /// <inheritdoc cref="DiscordSocketClient.GetGlobalApplicationCommandAsync(ulong, RequestOptions)"/>
    public static ValueTask<SocketApplicationCommand> GetGlobalApplicationCommandAsync(
        this DiscordShardedClient client,
        ulong id,
        RequestOptions requestOptions = null
    )
        => client.GetShard().GetGlobalApplicationCommandAsync(id, requestOptions);

    /// <inheritdoc cref="DiscordSocketClient.GetUserAsync(ulong, RequestOptions)"/>
    public static async ValueTask<IUser> GetUserAsync(
        this DiscordShardedClient client,
        ulong userId, 
        RequestOptions requestOptions = null
    )
        => await client.GetShard().GetUserAsync(userId, requestOptions);
}

#endif
