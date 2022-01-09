using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class InterleaveReceiverGroup
    {
        public InterleaveReceiverGroup(params ReceiverTask[] branches) => Branches = branches;

        internal readonly ReceiverTask[] Branches;
    }
}
