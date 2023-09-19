#if WE_LOVE_EM_SLASH_COMMANDS

namespace Grid.Bot.Interfaces;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Utility;
using Registries;

/// <summary>
/// Refers to a class that all slash commands must implement so that the <see cref="CommandRegistry"/> can parse and insert them into the <see cref="CommandRegistry"/>'s command handler state.
/// </summary>
public interface ISlashCommandHandler
{
    /// <summary>
    /// Refers to the description of the <see cref="ISlashCommandHandler"/>, optional.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Refers to the name of the <see cref="ISlashCommandHandler"/>
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Refers to if this <see cref="ISlashCommandHandler"/> should be publicly documented, if true, then only users in the <see cref="DiscordRolesSettings.AdminUserIds"/> list are allowed the see documentation for this <see cref="ISlashCommandHandler"/>.
    /// <br />
    /// <b>THIS DOES NOT MEAN IT CANNOT BE CALLED, PLEASE USE THE <see cref="AdminUtility.RejectIfNotAdminAsync(SocketMessage)"/> OR <see cref="AdminUtility.UserIsAdmin(IUser)"/> TO VALIDATE IF THE USER IS IN THE GROUP.</b>
    /// 
    /// <br />
    /// <br />
    /// WARNING: This is obsolete for Slash Commands as it's documented by default.
    /// </summary>
    bool IsInternal { get; }

    /// <summary>
    /// Refers to if this <see cref="ISlashCommandHandler"/> is enabled or not, only used in the <see cref="CommandRegistry"/>
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Refers to the options for this <see cref="ISlashCommandHandler"/>, these are the "arguments"
    /// </summary>
    SlashCommandOptionBuilder[] Options { get; }

    /// <summary>
    /// The actual callback to be invoked when the <see cref="ISlashCommandHandler"/> is found in the <see cref="CommandRegistry"/>, it returns a <see cref="Task"/> so it can have async style code in it.
    /// </summary>
    /// <param name="command">The raw command.</param>
    /// <returns>Returns as <see cref="Task"/> that can be awaited on.</returns>
    Task ExecuteAsync(SocketSlashCommand command);
}


#endif // WE_LOVE_EM_SLASH_COMMANDS
