using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GetAllSettings : IStateSpecificCommandHandler
    {
        public string CommandName => "Get All Bot Instance Settings";
        public string CommandDescription => "Attempts to list all of the bot's settings, if the command was invoked in " +
                                            "a public channel (a channel that is an instance of SocketGuildChannel) " +
                                            "and the setting 'AllowLogSettingsInPublicChannels' is disabled, it will throw.";
        public string[] CommandAliases => new[] { "getall", "getallsettings" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            if (message.IsInPublicChannel() && !global::MFDLabs.Grid.Bot.Properties.Settings.Default.AllowLogSettingsInPublicChannels)
            {
                await message.ReplyAsync("Are you sure you want to do that? This will log sensitive things!");
                return;
            }
            
            var props = global::MFDLabs.Grid.Bot.Properties.Settings.Default.Properties.Cast<SettingsProperty>();

            var builder = new EmbedBuilder().WithTitle("All Application Settings.");

            var embeds = new List<Embed>();
            var count = 0;

            foreach (var field in props)
            {
                if (count == 24)
                {
                    embeds.Add(builder.Build());
                    builder = new EmbedBuilder();
                    count = 0;
                }

                builder.AddField(
                    $"{field.Name} ({field.PropertyType})",
                    $"`{(global::MFDLabs.Grid.Bot.Properties.Settings.Default[field.Name])}`",
                    false
                );
                count++;
            }

            if (count < 24) embeds.Add(builder.Build());

            await message.ReplyAsync($"Echoeing back {props.Count()} settings in {Math.Floor((float) (props.Count() / 25))} group{(props.Count() > 1 ? "s" : "")}.");

            foreach (var embed in embeds)
            {
                await message.Channel.SendMessageAsync(
                    embed: embed
                );
            }
        }
    }
}
