using System.Threading.Tasks;
using MFDLabs.Analytics.Google;
using MFDLabs.Networking;

#if DEBUG || DEBUG_LOGGING_IN_PROD
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Events
{
    public static class OnLatencyUpdated
    {
        public static async Task Invoke(int oldLatency, int newLatency)
        {
            await GoogleAnalyticsManager.TrackNetworkEventAsync("KeepAlive",
                "LatencyUpdate",
                $"Received a latency update from the discord socket, old latency '{oldLatency}', new latency '{newLatency}'.");
#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Info(
                "Received a latency update from the discord socket, old latency '{0}', new latency '{1}'.",
                oldLatency,
                newLatency);
#endif
        }
    }
}
