using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class ConcurrentReceiverGroup
    {
        public ConcurrentReceiverGroup(params ReceiverTask[] branches)
        {
            this._branches = branches;
        }

        internal ReceiverTask[] _branches;
    }
}
