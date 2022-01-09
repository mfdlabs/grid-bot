using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;
using System;

namespace Microsoft.Ccr.Core
{
    public class TeardownReceiverGroup : InterleaveReceiverGroup
    {
        public TeardownReceiverGroup(params ReceiverTask[] branches) : base(branches)
        {
            if (branches == null)
            {
                throw new ArgumentNullException("branches");
            }
            foreach (ReceiverTask receiverTask in branches)
            {
                if (receiverTask == null)
                {
                    throw new ArgumentNullException("branches");
                }
                if (receiverTask.State == ReceiverTaskState.Persistent)
                {
                    throw new ArgumentOutOfRangeException("branches", Resource.TeardownBranchesCannotBePersisted);
                }
            }
        }
    }
}
