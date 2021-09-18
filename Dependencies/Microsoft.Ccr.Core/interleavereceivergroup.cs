using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class InterleaveReceiverGroup
    {
        public InterleaveReceiverGroup(params ReceiverTask[] branches)
        {
            this._branches = branches;
        }

        internal ReceiverTask[] _branches;
    }
}
