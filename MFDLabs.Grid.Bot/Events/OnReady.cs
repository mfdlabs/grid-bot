using System.Threading.Tasks;
using MFDLabs.Grid.Bot.Global;
using MFDLabs.Logging;
using MFDLabs.Text.Extensions;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnReady
    {
        internal static async Task Invoke()
        {
            SystemLogger.Singleton.Debug(
                "Bot ready as '{0}#{1}'",
                BotGlobal.Singleton.Client.CurrentUser.Username,
                BotGlobal.Singleton.Client.CurrentUser.Discriminator
            );

            await BotGlobal.Singleton.Client.SetStatusAsync(
                Settings.Singleton.BotGlobalUserStatus
            );

            if (!Settings.Singleton.BotGlobalStatusMessage.IsNullOrEmpty())
                await BotGlobal.Singleton.Client.SetGameAsync(
                    Settings.Singleton.BotGlobalStatusMessage,
                    Settings.Singleton.BotGlobalStreamURL,
                    Settings.Singleton.BotGlobalActivityType
                );
            return;
        }
    }
}
