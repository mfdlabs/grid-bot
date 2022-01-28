using System.Threading.Tasks;
using MFDLabs.Analytics.Google;
using MFDLabs.Concurrency;
using MFDLabs.Concurrency.Base;
using MFDLabs.Concurrency.Base.Async;
using MFDLabs.Concurrency.Base.Unsafe;

#if DEBUG || DEBUG_LOGGING_IN_PROD
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Plugins
{
    internal sealed class UnsafePacketMetricsPlugin : UnsafeBasePlugin<UnsafePacketMetricsPlugin>
    {
        public override unsafe PluginResult OnReceive(Concurrency.Unsafe.Packet* packet)
        {
            GoogleAnalyticsManager.TrackNetworkEvent("Concurrency",
                "PacketReceived",
                $"Received unsafe result packet '{packet->Id}' with the sequence '{packet->SequenceId}' at '{packet->Created}' with the status of '{packet->Status}'");
            if (packet->Data != null)
            {
                GoogleAnalyticsManager.TrackNetworkEvent("Concurrency",
                    "PacketReceived",
                    $"The packet '{packet->Id}' with the sequence '{packet->SequenceId}' had an item with the typeof '{packet->Data->GetType().FullName}'");
            }
#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Info(
                "Received result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'",
                packet->Id,
                packet->SequenceId,
                packet->Created,
                packet->Status);

            if (packet->Data != null)
            {
                SystemLogger.Singleton.Info(
                    "The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'",
                    packet->Id,
                    packet->SequenceId,
                    packet->Data->GetType()
                        .FullName);
            }
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class PacketMetricsPlugin<TItem> : BasePlugin<PacketMetricsPlugin<TItem>, TItem>
        where TItem : class
    {
        public override PluginResult OnReceive(ref Packet<TItem> packet)
        {
            GoogleAnalyticsManager.TrackNetworkEvent("Concurrency",
                "PacketReceived",
                $"Received result packet '{packet.Id}' with the sequence '{packet.SequenceId}' at '{packet.Created}' with the status of '{packet.Status}'");
            if (packet.Item != null)
            {
                GoogleAnalyticsManager.TrackNetworkEvent("Concurrency",
                    "PacketReceived",
                    $"The packet '{packet.Id}' with the sequence '{packet.SequenceId}' had an item with the typeof '{packet.Item.GetType().FullName}'");
            }
#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Info(
                "Received result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'",
                packet.Id,
                packet.SequenceId,
                packet.Created,
                packet.Status);

            if (packet.Item != null)
            {
                SystemLogger.Singleton.Info(
                    "The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'",
                    packet.Id,
                    packet.SequenceId,
                    packet.Item.GetType()
                        .FullName);
            }
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class PacketMetricsPlugin : BasePlugin<PacketMetricsPlugin>
    {
        public override PluginResult OnReceive(ref Packet packet)
        {
            GoogleAnalyticsManager.TrackNetworkEvent("Concurrency",
                "PacketReceived",
                $"Received result packet '{packet.Id}' with the sequence '{packet.SequenceId}' at '{packet.Created}' with the status of '{packet.Status}'");
#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Info(
                "Received default packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'",
                packet.Id,
                packet.SequenceId,
                packet.Created,
                packet.Status);
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class AsyncPacketMetricsPlugin<TItem> : AsyncBasePlugin<AsyncPacketMetricsPlugin<TItem>, TItem>
        where TItem : class
    {
        public override async Task<PluginResult> OnReceive(Packet<TItem> packet)
        {
            await GoogleAnalyticsManager.TrackNetworkEventAsync("Concurrency",
                "PacketReceived",
                $"Received result packet '{packet.Id}' with the sequence '{packet.SequenceId}' at '{packet.Created}' with the status of '{packet.Status}'");
            if (packet.Item != null)
            {
                await GoogleAnalyticsManager.TrackNetworkEventAsync("Concurrency",
                    "PacketReceived",
                    $"The packet '{packet.Id}' with the sequence '{packet.SequenceId}' had an item with the typeof '{packet.Item.GetType().FullName}'");
            }
#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Info(
                "Received async result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'",
                packet.Id,
                packet.SequenceId,
                packet.Created,
                packet.Status);

            if (packet.Item != null)
            {
                SystemLogger.Singleton.Info(
                    "The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'",
                    packet.Id,
                    packet.SequenceId,
                    packet.Item.GetType()
                        .FullName);
            }
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class AsyncPacketMetricsPlugin : AsyncBasePlugin<AsyncPacketMetricsPlugin>
    {
        public override async Task<PluginResult> OnReceive(Packet packet)
        {
            await GoogleAnalyticsManager.TrackNetworkEventAsync("Concurrency",
                "PacketReceived",
                $"Received result packet '{packet.Id}' with the sequence '{packet.SequenceId}' at '{packet.Created}' with the status of '{packet.Status}'");
#if DEBUG || DEBUG_LOGGING_IN_PROD
            SystemLogger.Singleton.Info(
                "Received default async packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'",
                packet.Id,
                packet.SequenceId,
                packet.Created,
                packet.Status);
#endif
            return PluginResult.ContinueProcessing;
        }
    }
}
