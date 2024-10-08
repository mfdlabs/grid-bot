namespace Grid.Bot.Extensions;

using Discord;
using Discord.WebSocket;

using Threading.Extensions;

/// <summary>
/// Extension methods for <see cref="SocketInteraction" />
/// </summary>
public static class SocketInteractionExtensions
{
    /// <summary>
    /// Gets the <see cref="ISocketMessageChannel" /> from the <see cref="SocketInteraction" />, taking private threads into consideration.
    /// </summary>
    /// <param name="interaction">The current <see cref="SocketInteraction" /></param>
    /// <returns>An <see cref="ISocketMessageChannel" /></returns>
    public static IMessageChannel GetChannel(this SocketInteraction interaction) 
        => interaction.Channel ?? interaction.InteractionChannel;

    /// <summary>
    /// Gets the channel from the <see cref="SocketInteraction" />, taking private threads into consideration.
    /// </summary>
    /// <param name="interaction">The current <see cref="SocketInteraction"/></param>
    /// <returns>A string version of either <see cref="ISocketMessageChannel"/> or <see cref="IMessageChannel"/></returns>
    public static string GetChannelAsString(this SocketInteraction interaction) 
        => interaction.GetChannel().ToString();

    /// <summary>
    /// Gets an <see cref="IGuild"/> for a specific <see cref="SocketInteraction"/>, taking private threads into consideration.
    /// </summary>
    /// <param name="interaction"></param>
    /// <param name="client"></param>
    /// <returns></returns>
    public static IGuild GetGuild(this SocketInteraction interaction, IDiscordClient client)
    {
        if (interaction.GuildId == null) return null;

        if (interaction.Channel is SocketGuildChannel guildChannel)
            return guildChannel.Guild;

        return client.GetGuildAsync(interaction.GuildId.Value).SyncOrDefault();
    }
}