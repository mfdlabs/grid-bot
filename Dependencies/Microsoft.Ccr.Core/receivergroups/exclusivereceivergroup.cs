using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class ExclusiveReceiverGroup
    {
        public ExclusiveReceiverGroup(params ReceiverTask[] branches) => Branches = branches;
        internal readonly ReceiverTask[] Branches;
    }
}
