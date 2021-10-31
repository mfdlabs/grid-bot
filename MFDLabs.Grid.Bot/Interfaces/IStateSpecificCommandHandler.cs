using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Interfaces
{
    /// <summary>
    /// Refers to a class that all commands must implement so that the <see cref="CommandRegistry"/> can parse and insert them into the <see cref="CommandRegistry"/>'s command handler state.
    /// </summary>
    public interface IStateSpecificCommandHandler
    {
        /// <summary>
        /// Refers to the name of the <see cref="IStateSpecificCommandHandler"/> to be read out in a <see cref="CommandRegistry"/> help embed <see cref="CommandRegistry.ConstructHelpEmbedForAllCommands(IUser)"/> or <see cref="CommandRegistry.ConstructHelpEmbedForSingleCommand(string, IUser)"/>.
        /// </summary>
        string CommandName { get; }

        /// <summary>
        /// Refers to the description of the <see cref="IStateSpecificCommandHandler"/>, optional, that is read out in a <see cref="CommandRegistry"/> help embed (<see cref="CommandRegistry.ConstructHelpEmbedForAllCommands(IUser)"/> or <see cref="CommandRegistry.ConstructHelpEmbedForSingleCommand(string, IUser)"/>).
        /// </summary>
        string CommandDescription { get; }

        /// <summary>
        /// Refers to the command names that invoke this <see cref="IStateSpecificCommandHandler"/>, like <code language="cs">new string[] { "h", "help" }</code>
        /// </summary>
        string[] CommandAliases { get; }

        /// <summary>
        /// Refers to if this <see cref="IStateSpecificCommandHandler"/> should be publicly documented, if true, then only users in the <see cref="MFDLabs.Grid.Bot.Properties.Settings.Admins"/> list are allowed the see documentation for this <see cref="IStateSpecificCommandHandler"/>.
        /// <br />
        /// <b>THIS DOES NOT MEAN IT CANNOT BE CALLED, PLEASE USE THE <see cref="AdminUtility.RejectIfNotAdminAsync(SocketMessage)"/>, <see cref="AdminUtility.UserIsAdmin(string)"/> <see cref="AdminUtility.UserIsAdmin(IUser)"/>, OR <see cref="AdminUtility.UserIsAdmin(ulong)"/> TO VALIDATE IF THE USER IS IN THE GROUP.</b>
        /// </summary>
        bool Internal { get; }

        /// <summary>
        /// Refers to if this <see cref="IStateSpecificCommandHandler"/> is enabled or not, only used in the <see cref="CommandRegistry"/>
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// The actual callback to be invoked when the <see cref="IStateSpecificCommandHandler"/> is found in the <see cref="CommandRegistry"/>, it returns a <see cref="Task"/> so it can have async style code in it.
        /// </summary>
        /// <param name="messageContentArray">The parameters of the <see cref="IStateSpecificCommandHandler"/>, normally they take the <see cref="IStateSpecificCommandHandler"/> name of the <see cref="IStateSpecificCommandHandler"/> out and push the rest to an array.</param>
        /// <param name="message">The <see cref="SocketMessage"/> that is used to reply to the frontend user.</param>
        /// <param name="originalAlias">The original <see cref="IStateSpecificCommandHandler"/> name, as you may want to do something different per <see cref="IStateSpecificCommandHandler"/> name.</param>
        /// <returns>Returns as <see cref="Task"/> that can be awaited on.</returns>
        Task Invoke(string[] messageContentArray, SocketMessage message, string originalAlias);
    }
}
