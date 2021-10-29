using System;
using Microsoft.Ccr.Core;

namespace MFDLabs.Concurrency
{
    /// <inheritdoc/>
    public class Interleaver
    {
        readonly Port<Action> exclusive = new Port<Action>();
        readonly Port<Action> concurrent = new Port<Action>();

        /// <inheritdoc/>
        public void DoExclusive(Action action)
        {
            exclusive.Post(action);
        }

        /// <inheritdoc/>
        public void DoConcurrent(Action action)
        {
            concurrent.Post(action);
        }

        /// <inheritdoc/>
        public Interleaver()
        {
            ConcurrencyService.Singleton.Activate(Arbiter.Interleave(
                new TeardownReceiverGroup(),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive(true, exclusive, action => action())
                    ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive(true, concurrent, action => action())
                    )
                ));
        }
    }
}
