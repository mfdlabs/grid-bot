#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class UpdateBlacklist : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Adds or removes a user to/from the blacklist.";
        public string CommandAlias => "blacklist";
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add user to the blacklist")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "User to add to the blacklist", true),
            new SlashCommandOptionBuilder()
                .WithName("remove")
                .WithDescription("Add user to the blacklist")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "User to remove from the blacklist", true)
        };

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!await command.RejectIfNotAdminAsync()) return;

            var subCommand = command.Data.GetSubCommand();

            var userOption = (IUser)subCommand.GetOptionValue("user");
            if (userOption == null)
            {
                await command.RespondEphemeralPingAsync("The user cannot be null");
                return;
            }

            if (userOption.IsBot || userOption.IsWebhook)
            {
                await command.RespondEphemeralPingAsync("Cannot update the status of a bot user.");
                return;
            }

            if (userOption.IsOwner())
            {
                await command.RespondEphemeralPingAsync("Cannot update the status of user because they are the owner.");
                return;
            }


            switch (subCommand.Name)
            {
                case "add":
                    if (userOption.IsBlacklisted()) { await command.RespondEphemeralPingAsync($"The user '{userOption.Id}' is already blacklisted!"); return; }
                    userOption.Blacklist();
                    await command.RespondEphemeralPingAsync($"Successfully added '{userOption.Id}' to the blacklist!");
                    return;
                case "remove":
                    if (!userOption.IsBlacklisted()) { await command.RespondEphemeralPingAsync($"The user '{userOption.Id}' is not blacklisted!"); return; }
                    userOption.Whitelist();
                    await command.RespondEphemeralPingAsync($"Successfully removed '{userOption.Id}' from the blacklist!");
                    return;
            }
        }
    }
}

#endif