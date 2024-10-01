namespace Grid.Bot.Utility;

using System.Threading.Tasks;

using Discord;
using Discord.Commands;

/// <summary>
/// Handles logging the contents of scripts to a Discord webhook.
/// </summary>
/// <remarks>
/// Logs in the following format:<br/><br/>
/// {attachment...}<br/>
/// {begin_embed}<br/>
/// **User:** {userInfo}<br/>
/// **Guild:** {guildInfo}<br/>
/// **Channel:** {channelInfo}<br/>
/// **Script Hash:** {scriptHash}<br/>
/// {end_embed}<br/>
/// </remarks>
public interface IScriptLogger
{
    /// <summary>
    /// Logs the contents of a script to a Discord webhook.
    /// </summary>
    /// <param name="script">The script to log.</param>
    /// <param name="context">The <see cref="IInteractionContext"/> to use.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LogScriptAsync(string script, IInteractionContext context);

    /// <summary>
    /// Logs the contents of a script to a Discord webhook.
    /// </summary>
    /// <param name="script">The script to log.</param>
    /// <param name="context">The <see cref="ICommandContext"/> to use.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task LogScriptAsync(string script, ICommandContext context);
}
