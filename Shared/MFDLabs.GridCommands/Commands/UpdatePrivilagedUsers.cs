﻿using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class UpdatePrivilagedUsers : IStateSpecificCommandHandler
    {
        public string CommandName => "Update Privilaged Users List";
        public string CommandDescription => "Updates the privilaged users list. " +
                                            "This is not persistent, unless this instance has either of the " +
                                            "following enviroment variables defined: WE_ON_THE_GRID, WE_ON_THE_RUN, " +
                                            "WE_ARE_AN_ACTOR\nLayout: " +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}updateprivilagedusers " +
                                            $"add/remove userMention|userID";
        public string[] CommandAliases => new[] { "upusers", "updateprivilagedusers" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var subCommand = messageContentArray.ElementAtOrDefault(0);

            if (subCommand.IsNullOrWhiteSpace())
            {
                await message.ReplyAsync("The sub command is required to be either 'add' or 'remove'.");
                return;
            }

            string user = messageContentArray.ElementAtOrDefault(1);
            bool wasMention = false;

            if (!ulong.TryParse(user, out var uid))
            {
                if (message.MentionedUsers.Count != 0)
                {
                    wasMention = true;
                    var mention = message.MentionedUsers.ElementAt(0);
                    if (mention.IsBot)
                    {
                        await message.ReplyAsync("Cannot update the status of a bot user.");
                        return;
                    }
                    if (mention.IsOwner())
                    {
                        await message.ReplyAsync("Cannot update the status of user because they are the owner.");
                        return;
                    }
                    user = mention.Id.ToString();
                }
                else
                {
                    await message.ReplyAsync(
                        $"The user of '{(user == null ? "Null User" : user.Escape().EscapeNewLines())}'" +
                        $" was not a valid '{typeof(long)}' or was not a valid mention.");
                    return;
                }
            }
            else
            {
                user = uid.ToString();
            }

            if (!wasMention)
            {
                IUser u;
                if ((u = await BotGlobal.Client.GetUserAsync(uid)) == null)
                {
                    await message.ReplyAsync($"The user '{uid}' was not found.");
                    return;
                }

                if (u.IsOwner())
                {
                    await message.ReplyAsync("Cannot update the status of user because they are the owner.");
                    return;
                }

                if (u.IsBot)
                {
                    await message.ReplyAsync("Cannot update the status of a bot user.");
                    return;
                }
            }

            var higherPrivilagedUsers = global::MFDLabs.Grid.Bot.Properties.Settings.Default.HigherPrivilagedUsers.Split(',').ToList();


            switch (subCommand?.ToLowerInvariant())
            {
            case "add":
                if (!higherPrivilagedUsers.Contains(user))
                {
                    higherPrivilagedUsers.Add(user);
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default["HigherPrivilagedUsers"] = higherPrivilagedUsers.Join(',');
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
                    await message.ReplyAsync($"Successfully added '{user}' to the privilaged users whitelist.");
                    break;
                }
                await message.ReplyAsync($"The user '{user}' is already a higher privilaged user, " +
                                         $"if you want to remove them, please re-run this command " +
                                         $"like: '{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}{originalCommand} remove {user}'.");
                break;
            case "remove":
                if (higherPrivilagedUsers.Contains(user))
                {
                    higherPrivilagedUsers.Remove(user);
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default["HigherPrivilagedUsers"] = higherPrivilagedUsers.Join(',');
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.Save();
                    await message.ReplyAsync($"Successfully removed '{user}' from the privilaged users whitelist.");
                    break;
                }
                await message.ReplyAsync($"The user '{user}' is not a higher privilaged user, " +
                                         $"if you want to add them, please re-run this command like: " +
                                         $"'{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}{originalCommand} add {user}'.");
                break;
            default:
                await message.ReplyAsync($"Unknown subcommand '{subCommand}', the allowed subcommands are: 'add', 'remove'.");
                break;
            }
        }
    }
}