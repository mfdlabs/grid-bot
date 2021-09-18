using MFDLabs.Logging;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnLoggedOut
    {
        internal static Task Invoke()
        {
            SystemLogger.Singleton.Debug("BotGlobal logged out.");
            return Task.CompletedTask;
        }
    }
}
