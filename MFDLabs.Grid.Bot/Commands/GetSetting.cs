using Discord;
using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class GetSetting : IStateSpecificCommandHandler
    {
        public string CommandName => "Get Setting";

        public string CommandDescription => "Tries to get the setting by name. NOT CASE SENSITIVE";

        public string[] CommandAliases => new string[] { "get", "getsetting" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            var settingName = messageContentArray.ElementAtOrDefault(0);

            if (settingName.IsNullOrEmpty())
            {
                SystemLogger.Singleton.Warning("Null Setting name, aborting.");
                await message.ReplyAsync("The first parameter of the command was null, expected the \"SettingName\" to be not null or not empty.");
                return;
            }

            object setting;

            try
            {
                setting = global::MFDLabs.Grid.Bot.Properties.Settings.Default[settingName];
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
