﻿/* Copyright MFDLABS Corporation. All rights reserved. */

#if WE_LOVE_EM_SLASH_COMMANDS

using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Diagnostics;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Grid.Bot.WorkQueues;

namespace MFDLabs.Grid.Bot.SlashCommands
{
    internal class Render : IStateSpecificSlashCommandHandler
    {
        public string CommandDescription => "Renders a roblox user.";
        public string CommandAlias => "render";
        public bool Internal => false;
        public bool IsEnabled { get; set; } = true;
        public ulong? GuildId => null;
        public SlashCommandOptionBuilder[] Options => new[]
        {
            new SlashCommandOptionBuilder()
                .WithName("roblox_id")
                .WithDescription("Render a user by their Roblox User ID.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("id", ApplicationCommandOptionType.Integer, "The user ID of the Roblox user.", true, minValue: 1, maxValue: global::MFDLabs.Grid.Bot.Properties.Settings.Default.MaxUserIDSize),
            new SlashCommandOptionBuilder()
                .WithName("roblox_name")
                .WithDescription("Render a user by their Roblox Username.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user_name", ApplicationCommandOptionType.String, "The user name of the Roblox user.", true),
            new SlashCommandOptionBuilder()
                .WithName("discord_user")
                .WithDescription("Render a user by their Discord Account.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("user", ApplicationCommandOptionType.User, "The user ref to render.", true),
            new SlashCommandOptionBuilder()
                .WithName("self")
                .WithDescription("Render yourself!")
                .WithType(ApplicationCommandOptionType.SubCommand),
        };

        public async Task Invoke(SocketSlashCommand command)
        {
            if (!PercentageInvoker.InvokeAction(
                () => RenderingWorkQueueV2.Singleton.EnqueueWorkItem(command),
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.RenderWorkQueueRolloutPercentage
            ))
            {
                if (command.User.IsAdmin())
                {
                    RenderingWorkQueueV2.Singleton.EnqueueWorkItem(command);
                    return;
                }
                await command.RespondEphemeralPingAsync("Rendering is not enabled at this time.");
                return;
            }
        }
    }
}

#endif