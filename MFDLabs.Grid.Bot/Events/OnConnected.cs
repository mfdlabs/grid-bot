using System.Threading.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnConnected
    {
        internal static Task Invoke()
        {
            SystemLogger.Singleton.Debug("Client has been connected to the Hub.");
            return Task.CompletedTask;
        }
    }
}
