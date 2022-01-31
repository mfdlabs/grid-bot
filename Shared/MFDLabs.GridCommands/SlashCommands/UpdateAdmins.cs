﻿#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal sealed class UpdateAdmins : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Adds or removes a user to/from the admins user list.";
        public string CommandAlias => "admins";
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("add")
                .WithDescription("Add user to the admins user list")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "The user to add", true),
            new SlashCommandOptionBuilder()
                .WithName("remove")
                .WithDescription("Remove user from the admins user list")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "The user to remove", true)
        };

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!await command.RejectIfNotAdminAsync()) return;

            using (await command.DeferEphemeralAsync())
            {

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
                        if (userOption.IsBlacklisted()) { await command.RespondEphemeralPingAsync($"The user '{userOption.Id}' is already on the admins user list!"); return; }
                        userOption.Promote();
                        await command.RespondEphemeralPingAsync($"Successfully added '{userOption.Id}' to the admins user list!");
                        return;
                    case "remove":
                        if (!userOption.IsBlacklisted()) { await command.RespondEphemeralPingAsync($"The user '{userOption.Id}' is not on the admins user list!"); return; }
                        userOption.Demote();
                        await command.RespondEphemeralPingAsync($"Successfully removed '{userOption.Id}' from the admins user list!");
                        return;
                }
            }
        }
    }
}

#endif