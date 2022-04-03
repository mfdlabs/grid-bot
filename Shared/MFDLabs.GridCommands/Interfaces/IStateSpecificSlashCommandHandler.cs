/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Grid.Bot.Utility;

namespace MFDLabs.Grid.Bot.Interfaces
{

    /// <summary>
    /// Refers to a class that all slash commands must implement so that the <see cref="CommandRegistry"/> can parse and insert them into the <see cref="CommandRegistry"/>'s command handler state.
    /// </summary>
    public interface IStateSpecificSlashCommandHandler
    {
        /// <summary>
        /// Refers to the description of the <see cref="IStateSpecificSlashCommandHandler"/>, optional, that is read out in a <see cref="CommandRegistry"/> help embed (<see cref="CommandRegistry.ConstructHelpEmbedForAllSlashCommands(IUser)"/> or <see cref="CommandRegistry.ConstructHelpEmbedForSingleSlashCommand(string, IUser)"/>).
        /// </summary>
        string CommandDescription { get; }

        /// <summary>
        /// Refers to the name of the <see cref="IStateSpecificSlashCommandHandler"/>
        /// </summary>
        string CommandAlias { get; }

        /// <summary>
        /// Refers to if this <see cref="IStateSpecificSlashCommandHandler"/> should be publicly documented, if true, then only users in the <see cref="MFDLabs.Grid.Bot.Properties.Settings.Admins"/> list are allowed the see documentation for this <see cref="IStateSpecificSlashCommandHandler"/>.
        /// <br />
        /// <b>THIS DOES NOT MEAN IT CANNOT BE CALLED, PLEASE USE THE <see cref="AdminUtility.RejectIfNotAdminAsync(SocketMessage)"/>, <see cref="AdminUtility.UserIsAdmin(string)"/> <see cref="AdminUtility.UserIsAdmin(IUser)"/>, OR <see cref="AdminUtility.UserIsAdmin(ulong)"/> TO VALIDATE IF THE USER IS IN THE GROUP.</b>
        /// 
        /// <br />
        /// <br />
        /// WARNING: This is obsolete for Slash Commands as it's documented by default.
        /// </summary>
        bool Internal { get; }

        /// <summary>
        /// Refers to if this <see cref="IStateSpecificSlashCommandHandler"/> is enabled or not, only used in the <see cref="CommandRegistry"/>
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Refers to the Guild ID, if this is set, this <see cref="IStateSpecificSlashCommandHandler"/> will be scoped to the guild of that ID, it will throw if the guild is not found!
        /// </summary>
        ulong? GuildId { get; }

        /// <summary>
        /// Refers to the options for this <see cref="IStateSpecificSlashCommandHandler"/>, these are the "arguments"
        /// </summary>
        SlashCommandOptionBuilder[] Options { get; }

        /// <summary>
        /// The actual callback to be invoked when the <see cref="IStateSpecificSlashCommandHandler"/> is found in the <see cref="CommandRegistry"/>, it returns a <see cref="Task"/> so it can have async style code in it.
        /// </summary>
        /// <param name="command">The raw command.</param>
        /// <returns>Returns as <see cref="Task"/> that can be awaited on.</returns>
        Task Invoke(SocketSlashCommand command);
    }

}

#endif // WE_LOVE_EM_SLASH_COMMANDS