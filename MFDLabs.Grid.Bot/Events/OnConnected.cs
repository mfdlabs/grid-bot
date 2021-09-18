using MFDLabs.Logging;
using System.Threading.Tasks;

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
