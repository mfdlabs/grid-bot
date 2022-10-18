using System.Threading.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnLoggedIn
    {
        public static Task Invoke()
        {
            Logger.Singleton.Debug("BotGlobal logged in.");
            return Task.CompletedTask;
        }
    }
}
