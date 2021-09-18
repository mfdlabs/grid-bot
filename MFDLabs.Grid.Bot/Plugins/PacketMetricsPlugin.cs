using MFDLabs.Concurrency;
using MFDLabs.Concurrency.Base;
using MFDLabs.Concurrency.Base.Async;
using MFDLabs.Logging;
using System.Threading.Tasks;

namespace MFDLabs.Grid.Bot.Plugins
{
    internal sealed class PacketMetricsPlugin<TItem> : BasePlugin<PacketMetricsPlugin<TItem>, TItem>
        where TItem : class
    {
        public override PluginResult OnReceive(ref Packet<TItem> packet)
        {
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
#if DEBUG
            SystemLogger.Singleton.Info("Received default packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);
#endif
            return PluginResult.ContinueProcessing;
        }
    }

    internal sealed class AsyncPacketMetricsPlugin<TItem> : AsyncBasePlugin<AsyncPacketMetricsPlugin<TItem>, TItem>
        where TItem : class
    {
        public override Task<PluginResult> OnReceive(Packet<TItem> packet)
        {
#if DEBUG
            SystemLogger.Singleton.Info("Received async result packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);

            if (packet.Item != null)
            {
                SystemLogger.Singleton.Info("The packet '{0}' with the sequence '{1}' had an item with the typeof '{2}'", packet.ID, packet.SequenceID, packet.Item.GetType().FullName);
            }
#endif
            return Task.FromResult(PluginResult.ContinueProcessing);
        }
    }

    internal sealed class AsyncPacketMetricsPlugin : AsyncBasePlugin<AsyncPacketMetricsPlugin>
    {
        public override Task<PluginResult> OnReceive(Packet packet)
        {
#if DEBUG
            SystemLogger.Singleton.Info("Received default async packet '{0}' with the sequence '{1}' at '{2}' with the status of '{3}'", packet.ID, packet.SequenceID, packet.Created, packet.Status);
#endif
            return Task.FromResult(PluginResult.ContinueProcessing);
        }
    }
}
