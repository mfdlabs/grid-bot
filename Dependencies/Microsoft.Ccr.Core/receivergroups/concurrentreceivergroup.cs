using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class ConcurrentReceiverGroup
    {
        public ConcurrentReceiverGroup(params ReceiverTask[] branches) => Branches = branches;

        internal readonly ReceiverTask[] Branches;
    }
}
