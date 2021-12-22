using System.Threading.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    internal static class OnLoggedOut
    {
        internal static Task Invoke()
        {
            SystemLogger.Singleton.Debug("BotGlobal logged out.");
            return Task.CompletedTask;
        }
    }
}
