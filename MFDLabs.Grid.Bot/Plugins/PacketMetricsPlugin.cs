using System.Threading.Tasks;
using MFDLabs.Analytics.Google;
using MFDLabs.Concurrency;
using MFDLabs.Concurrency.Base;
using MFDLabs.Concurrency.Base.Async;
using MFDLabs.Concurrency.Base.Unsafe;

#if DEBUG
using MFDLabs.Logging;
#endif

namespace MFDLabs.Grid.Bot.Plugins
{
    internal sealed class UnsafePacketMetricsPlugin : UnsafeBasePlugin<UnsafePacketMetricsPlugin>
    {
        public override unsafe PluginResult OnReceive(Concurrency.Unsafe.Packet* packet)
        {
            GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Concurrency", "PacketReceived", $"Received unsafe result packet '{packet->id}' with the sequence '{packet->sequence_id}' at '{packet->created}' with the status of '{packet->status}'");
            if (packet->data != null)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Concurrency", "PacketReceived", $"The packet '{packet->id}' with the sequence '{packet->sequence_id}' had an item with the typeof '{packet->data->GetType().FullName}'");
            }
#if DEBUG
            SystemLogger.Singleton.Info("Received result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet->id, packet->sequence_id, packet->created, packet->status);

            if (packet->data != null)
            {
                SystemLogger.Singleton.Info("The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'", packet->id, packet->sequence_id, packet->data->GetType().FullName);
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
            GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Concurrency", "PacketReceived", $"Received result packet '{packet.ID}' with the sequence '{packet.SequenceID}' at '{packet.Created}' with the status of '{packet.Status}'");
            if (packet.Item != null)
            {
                GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Concurrency", "PacketReceived", $"The packet '{packet.ID}' with the sequence '{packet.SequenceID}' had an item with the typeof '{packet.Item.GetType().FullName}'");
            }
#if DEBUG
            SystemLogger.Singleton.Info("Received result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);

            if (packet.Item != null)
            {
                SystemLogger.Singleton.Info("The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'", packet.ID, packet.SequenceID, packet.Item.GetType().FullName);
            }
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class PacketMetricsPlugin : BasePlugin<PacketMetricsPlugin>
    {
        public override PluginResult OnReceive(ref Packet packet)
        {
            GoogleAnalyticsManager.Singleton.TrackNetworkEvent("Concurrency", "PacketReceived", $"Received result packet '{packet.ID}' with the sequence '{packet.SequenceID}' at '{packet.Created}' with the status of '{packet.Status}'");
#if DEBUG
            SystemLogger.Singleton.Info("Received default packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class AsyncPacketMetricsPlugin<TItem> : AsyncBasePlugin<AsyncPacketMetricsPlugin<TItem>, TItem>
        where TItem : class
    {
        public override async Task<PluginResult> OnReceive(Packet<TItem> packet)
        {
            await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("Concurrency", "PacketReceived", $"Received result packet '{packet.ID}' with the sequence '{packet.SequenceID}' at '{packet.Created}' with the status of '{packet.Status}'");
            if (packet.Item != null)
            {
                await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("Concurrency", "PacketReceived", $"The packet '{packet.ID}' with the sequence '{packet.SequenceID}' had an item with the typeof '{packet.Item.GetType().FullName}'");
            }
#if DEBUG
            SystemLogger.Singleton.Info("Received async result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);

            if (packet.Item != null)
            {
                SystemLogger.Singleton.Info("The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'", packet.ID, packet.SequenceID, packet.Item.GetType().FullName);
            }
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class AsyncPacketMetricsPlugin : AsyncBasePlugin<AsyncPacketMetricsPlugin>
    {
        public override async Task<PluginResult> OnReceive(Packet packet)
        {
            await GoogleAnalyticsManager.Singleton.TrackNetworkEventAsync("Concurrency", "PacketReceived", $"Received result packet '{packet.ID}' with the sequence '{packet.SequenceID}' at '{packet.Created}' with the status of '{packet.Status}'");
#if DEBUG
            SystemLogger.Singleton.Info("Received default async packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);
#endif
            return PluginResult.ContinueProcessing;
        }
    }
}
