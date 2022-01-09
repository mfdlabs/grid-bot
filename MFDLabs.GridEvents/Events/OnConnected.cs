using System.Threading.Tasks;
using MFDLabs.Logging;

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnConnected
    {
        public static Task Invoke()
        {
            SystemLogger.Singleton.Debug("Client has been connected to the Hub.");
            return Task.CompletedTask;
        }
    }
}
