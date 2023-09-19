namespace Grid.Bot.Global;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Logging;

/// <summary>
/// A class that exposes the current Discord client.
/// </summary>
public static class BotRegistry
{
    /// <summary>
    /// Is the client ready?
    /// </summary>
    public static bool Ready { get; set; }

    /// <summary>
    /// Gets or sets the client.
    /// </summary>
    public static DiscordShardedClient Client { get; set; }
}
