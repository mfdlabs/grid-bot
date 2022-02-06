using System;
using Microsoft.Ccr.Core.Arbiters;
using Microsoft.Ccr.Core.Properties;

namespace Microsoft.Ccr.Core
{
    public class TeardownReceiverGroup : InterleaveReceiverGroup
    {
        public TeardownReceiverGroup(params ReceiverTask[] branches) 
            : base(branches)
        {
            if (branches == null) 
                throw new ArgumentNullException(nameof(branches));

            foreach (var branch in branches)
            {
                if (branch == null) 
                    throw new ArgumentNullException(nameof(branches));

                if (branch.State == ReceiverTaskState.Persistent) 
                    throw new ArgumentOutOfRangeException(nameof(branches), Resource.TeardownBranchesCannotBePersisted);
            }
        }
    }
}
