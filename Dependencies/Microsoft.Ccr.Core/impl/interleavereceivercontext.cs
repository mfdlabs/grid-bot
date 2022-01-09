using System.Collections.Generic;
using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    internal class InterleaveReceiverContext
    {
        public InterleaveReceiverContext(InterleaveReceivers receiverGroup) => ReceiverGroup = receiverGroup;

        public readonly InterleaveReceivers ReceiverGroup;
        public readonly Queue<Tuple<ITask, ReceiverTask>> PendingItems = new();
    }
}
