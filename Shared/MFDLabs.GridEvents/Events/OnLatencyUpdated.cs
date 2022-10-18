/* Copyright MFDLABS Corporation. All rights reserved. */

using System.Threading.Tasks;
using MFDLabs.Analytics.Google;

#if DEBUG || DEBUG_LOGGING_IN_PROD
using MFDLabs.Logging;
#endif

#if DISCORD_SHARDING_ENABLED
using Discord.WebSocket;
#endif

namespace MFDLabs.Grid.Bot.Events
{
#if DISCORD_SHARDING_ENABLED
    public static class OnShardLatencyUpdated
    {
        public static async Task Invoke(int oldLatency, int newLatency, DiscordSocketClient client)
        {
            await GoogleAnalyticsManager.TrackNetworkEventAsync("KeepAlive",
                "LatencyUpdate",
                $"Received a latency update from the discord socket, old latency '{oldLatency}', new latency '{newLatency}'.");
#if DEBUG || DEBUG_LOGGING_IN_PROD
            Logger.Singleton.Info(
                "Received a latency update from the discord socket, old latency '{0}', new latency '{1}'.",
                oldLatency,
                newLatency);
#endif
        }
    }
#else
    public static class OnLatencyUpdated
    {
        public static async Task Invoke(int oldLatency, int newLatency)
        {
            await GoogleAnalyticsManager.TrackNetworkEventAsync("KeepAlive",
                "LatencyUpdate",
                $"Received a latency update from the discord socket, old latency '{oldLatency}', new latency '{newLatency}'.");
#if DEBUG || DEBUG_LOGGING_IN_PROD
            Logger.Singleton.Info(
                "Received a latency update from the discord socket, old latency '{0}', new latency '{1}'.",
                oldLatency,
                newLatency);
#endif
        }
    }
#endif
}
