using System.Threading.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnLoggedOut
    {
        public static Task Invoke()
        {
            SystemLogger.Singleton.Debug("BotGlobal logged out.");
            return Task.CompletedTask;
        }
    }
}
