using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    public class ExclusiveReceiverGroup
    {
        public ExclusiveReceiverGroup(params ReceiverTask[] branches)
        {
            this._branches = branches;
        }

        internal ReceiverTask[] _branches;
    }
}
