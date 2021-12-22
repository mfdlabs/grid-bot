using System.Threading.Tasks;
using MFDLabs.Analytics.Google;
using MFDLabs.Networking;

#if DEBUG
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Events
{
    internal static class OnLatencyUpdated
    {
        internal static async Task Invoke(int oldLatency, int newLatency)
        {
            await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("KeepAlive",
                "LatencyUpdate",
                $"Received a latency update from the discord socket, old latency '{oldLatency}', new latency '{newLatency}'.");
#if DEBUG
            SystemLogger.Singleton.Info(
                "Received a latency update from the discord socket, old latency '{0}', new latency '{1}'.",
                oldLatency,
                newLatency);
#endif
        }
    }
}
