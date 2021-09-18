using MFDLabs.Logging;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Events
{
    internal sealed class OnLatencyUpdated
    {
        internal static Task Invoke(int oldLatency, int newLatency)
        {
#if DEBUG
            SystemLogger.Singleton.Info("Received a latency update from the discord socket, old latency '{0}', new latency '{1}'.", oldLatency, newLatency);
#endif
            return Task.CompletedTask;
        }
    }
}
