using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public interface IPortReceive
    {
        object Test();
        int ItemCount { get; }
        void RegisterReceiver(ReceiverTask receiver);
        void UnregisterReceiver(ReceiverTask receiver);
        ReceiverTask[] GetReceivers();
        object[] GetItems();
        void Clear();
    }
}
