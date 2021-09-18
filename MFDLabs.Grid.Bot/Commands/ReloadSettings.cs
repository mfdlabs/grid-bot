using Discord.WebSocket;
using MFDLabs.Grid.Bot.Extensions;
using MFDLabs.Grid.Bot.Interfaces;
using System.Reflection;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Commands
{
    internal sealed class ReloadSettings : IStateSpecificCommandHandler
    {
        public string CommandName => "Reload Settings";

        public string CommandDescription => "Reloads all settings from app.config. Please refrain from using this as it isn't thread safe.";

        public string[] CommandAliases => new string[] { "reload", "reloadsettings" };

        public bool Internal => true;

        public bool IsEnabled { get; set; } = true;

        public async Task Invoke(string[] messageContentArray, SocketMessage message, string originalCommand)
        {
            if (!await message.RejectIfNotAdminAsync()) return;

            global::MFDLabs.Grid.Bot.Properties.Settings.Default.Reload();
            await message.ReplyAsync($"Successfully reloaded all settings from {Assembly.GetExecutingAssembly().GetName().Name}.exe.config");
        }
    }
}
