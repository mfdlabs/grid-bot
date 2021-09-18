using MFDLabs.Logging;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnLoggedIn
    {
        internal static Task Invoke()
        {
            SystemLogger.Singleton.Debug("BotGlobal logged in.");
            return Task.CompletedTask;
        }
    }
}
