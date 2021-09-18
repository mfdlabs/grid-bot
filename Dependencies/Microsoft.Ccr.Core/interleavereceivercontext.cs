using Microsoft.Ccr.Core.Arbiters;
using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    internal class InterleaveReceiverContext
    {
        public InterleaveReceiverContext(InterleaveReceivers receiverGroup)
        {
            this.ReceiverGroup = receiverGroup;
        }

        public InterleaveReceivers ReceiverGroup;

        public Queue<Tuple<ITask, ReceiverTask>> PendingItems = new Queue<Tuple<ITask, ReceiverTask>>();
    }
}
