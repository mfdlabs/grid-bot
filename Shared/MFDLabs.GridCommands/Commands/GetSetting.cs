/* Copyright MFDLABS Corporation. All rights reserved. */

/*

TODO: We want a Vault and a ConsulKV (lol.) system in here?
It may or may not help, it depends on if you told them to set prop on prop change.
 
*/


using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GetSetting : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Bot Instance Setting";
        public string CommandDescription => $"Tries to get a setting from the " +
                                            $"'{typeof(global::MFDLabs.Grid.Bot.Properties.Settings).FullName}' " +
                                            $"by name, if it is not found it will return the raw exception message.\nLayout: " +
                                            $"{MFDLabs.Grid.Bot.Properties.Settings.Default.Prefix}getsetting settingName.";
        public string[] CommandAliases => new[] { "get", "getsetting" };
        public bool Internal => true;
        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var settingName = messageContentArray.ElementAtOrDefault(0);

            if (settingName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null Setting name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, expected " +
                                         "the \"SettingName\" to be not null or not empty.");
                return;
            }

            object setting;

            try
            {
                setting = global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName!];
            }
            catch (SettingsPropertyNotFoundException ex)
            {
                SystemLogger.Singleton.Warning(ex.Message);
                await message.ReplyAsync(ex.Message);
                return;
            }

            await message.Channel.SendMessageAsync(
                embed: new EmbedBuilder()
                        .WithTitle(settingName)
                        .WithDescription($"```\n{setting}\n```")
                        .WithColor(0x00, 0xff, 0x00)
                        .Build()
            );
        }
    }
}
