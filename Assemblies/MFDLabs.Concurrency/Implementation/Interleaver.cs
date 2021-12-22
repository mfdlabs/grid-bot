using System;
using Microsoft.Ccr.Core;

// ReSharper disable once CheckNamespace
namespace MFDLabs.Concurrency
{
    /// <summary>
    /// Simple interleave wrapper
    /// </summary>
    public class Interleaver
    {
        private readonly Port<Action> _exclusive = new Port<Action>();
        private readonly Port<Action> _concurrent = new Port<Action>();

        /// <summary>
        /// DoExclusive
        /// </summary>
        /// <param name="action"></param>
        public void DoExclusive(Action action) => _exclusive.Post(action);

        /// <summary>
        /// DoConcurrent
        /// </summary>
        /// <param name="action"></param>
        public void DoConcurrent(Action action) => _concurrent.Post(action);

        /// <summary>
        /// Construct new interleaver
        /// </summary>
        public Interleaver()
        {
            ConcurrencyService.Singleton.Activate(Arbiter.Interleave(
                new TeardownReceiverGroup(),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive(true, _exclusive, action => action())
                ),
                new ConcurrentReceiverGroup(
                    Arbiter.Receive(true, _concurrent, action => action())
                )
            ));
        }
    }
}