using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GetAllSettings : IStateSpecificCommandHandler
    {
        public string CommandName => "Get All Settings";

        public string CommandDescription => "Gets a list of settings with their types and values.";

        public string[] CommandAliases => new string[] { "getall", "getallsettings" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;
            if (message.IsInPublicChannel())
            {
                if (!Settings.Singleton.AllowLogSettingsInPublicChannels)
                {
                    await message.ReplyAsync("Are you sure you want to do that? This will log sensitive things!");
                    return;
                }
            }

            var fields = Settings.Singleton.GetType().GetProperties(BindingFlags.Instance | BindingFlags.NonPublic);

            EmbedBuilder embedBuilder = new EmbedBuilder().WithTitle("All Application Settings.");

            var embeds = new List<Embed>();
            var i = 0;

            foreach (var field in fields)
            {
                if (field.Name.ToLower().Contains("token") || field.Name.ToLower().Contains("accesskey") || field.Name.ToLower().Contains("apikey")) continue;
                if (i == 24)
                {
                    embeds.Add(embedBuilder.Build());
                    embedBuilder = new EmbedBuilder();
                    i = 0;
                }
                embedBuilder.AddField(
                    $"{field.Name} ({field.PropertyType.FullName})",
                    $"`{field.GetValue(Settings.Singleton)}`",
                    false
                );
                i++;
            }

            if (i < 24) embeds.Add(embedBuilder.Build());

            await message.ReplyAsync($"Echoeing back {fields.Length} settings in {Math.Floor((float)(fields.Length / 25))} group{(fields.Length > 1 ? "s" : "")}.");

            foreach (var embed in embeds)
            {
                await message.Channel.SendMessageAsync(
                    embed: embed
                );
            }
        }
    }
}
