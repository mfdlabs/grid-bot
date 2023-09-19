namespace Grid.Bot.Interfaces;

using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

using Utility;
using Registries;

/// <summary>
/// Refers to a class that all commands must implement so that the <see cref="CommandRegistry"/> can parse and insert them into the <see cref="CommandRegistry"/>'s command handler state.
/// </summary>
public interface ICommandHandler
{
    /// <summary>
    /// Refers to the name of the <see cref="ICommandHandler"/> to be read out in a <see cref="CommandRegistry"/> help embed <see cref="CommandRegistry.ConstructHelpEmbedForAllCommands(IUser)"/> or <see cref="CommandRegistry.ConstructHelpEmbedForSingleCommand(string, IUser)"/>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Refers to the description of the <see cref="ICommandHandler"/>, optional, that is read out in a <see cref="CommandRegistry"/> help embed (<see cref="CommandRegistry.ConstructHelpEmbedForAllCommands(IUser)"/> or <see cref="CommandRegistry.ConstructHelpEmbedForSingleCommand(string, IUser)"/>).
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Refers to the command names that invoke this <see cref="ICommandHandler"/>, like <code language="csharp">new string[] { "h", "help" }</code>
    /// </summary>
    string[] Aliases { get; }

    /// <summary>
    /// Refers to if this <see cref="ICommandHandler"/> should be publicly documented, if true, then only users in the <see cref="DiscordRolesSettings.AdminUserIds"/> list are allowed the see documentation for this <see cref="ICommandHandler"/>.
    /// <br />
    /// <b>THIS DOES NOT MEAN IT CANNOT BE CALLED, PLEASE USE THE <see cref="AdminUtility.RejectIfNotAdminAsync(SocketMessage)"/> OR <see cref="AdminUtility.UserIsAdmin(IUser)"/> TO VALIDATE IF THE USER IS IN THE GROUP.</b>
    /// </summary>
    bool IsInternal { get; }

    /// <summary>
    /// Refers to if this <see cref="ICommandHandler"/> is enabled or not, only used in the <see cref="CommandRegistry"/>
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// The actual callback to be invoked when the <see cref="ICommandHandler"/> is found in the <see cref="CommandRegistry"/>, it returns a <see cref="Task"/> so it can have async style code in it.
    /// </summary>
    /// <param name="messageContentArray">The parameters of the <see cref="ICommandHandler"/>, normally they take the <see cref="ICommandHandler"/> name of the <see cref="ICommandHandler"/> out and push the rest to an array.</param>
    /// <param name="message">The <see cref="SocketMessage"/> that is used to reply to the frontend user.</param>
    /// <param name="originalAlias">The original <see cref="ICommandHandler"/> name, as you may want to do something different per <see cref="ICommandHandler"/> name.</param>
    /// <returns>Returns as <see cref="Task"/> that can be awaited on.</returns>
    Task ExecuteAsync(string[] messageContentArray, SocketMessage message, string originalAlias);
}
