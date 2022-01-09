using System;
using System.Collections.Generic;
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
            
            throw new ApplicationException("Temporarily disabled until we figure out how to list settings inside an ApplicationSettingsBase instance");

            var fields = global::MFDLabs.Grid.Bot.Properties.Settings.Default.GetType()
                .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

            var builder = new EmbedBuilder().WithTitle("All Application Settings.");

            var embeds = new List<Embed>();
            var i = 0;

            foreach (var field in fields)
            {
                if (field.Name.ToLower()
                        .Contains("token") ||
                    field.Name.ToLower()
                        .Contains("accesskey") ||
                    field.Name.ToLower()
                        .Contains("apikey"))
                    continue;
                if (i == 24)
                {
                    embeds.Add(builder.Build());
                    builder = new EmbedBuilder();
                    i = 0;
                }
                builder.AddField(
                    $"{field.Name} ({field.PropertyType.FullName})",
                    $"`{field.GetValue(global::MFDLabs.Grid.Bot.Properties.Settings.Default)}`",
                    false
                );
                i++;
            }

            if (i < 24) embeds.Add(builder.Build());

            await message.ReplyAsync(
                $"Echoeing back {fields.Length} settings in {Math.Floor((float) (fields.Length / 25))} group{(fields.Length > 1 ? "s" : "")}.");

            foreach (var embed in embeds)
            {
                await message.Channel.SendMessageAsync(
                    embed: embed
                );
            }
        }
    }
}
