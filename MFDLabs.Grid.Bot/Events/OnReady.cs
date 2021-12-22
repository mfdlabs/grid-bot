using System.Threading.Tasks;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Grid.Bot.Registries;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Events
{
    internal static class OnReady
    {
        internal static async Task Invoke()
        {

            if (global::MFDLabs.Grid.Bot.Properties.Settings.Default.RegisterCommandRegistryAtAppStart)
                CommandRegistry.RegisterOnce();

            SystemLogger.Singleton.Debug(
                "Bot ready as '{0}#{1}'",
                BotGlobal.Client.CurrentUser.Username,
                BotGlobal.Client.CurrentUser.Discriminator
            );

            await BotGlobal.Client.SetStatusAsync(
                global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalUserStatus
            );

            if (!global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage.IsNullOrEmpty())
                await BotGlobal.Client.SetGameAsync(
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStatusMessage,
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalStreamURL,
                    global::MFDLabs.Grid.Bot.Properties.Settings.Default.BotGlobalActivityType
                );
            return;
        }
    }
}
