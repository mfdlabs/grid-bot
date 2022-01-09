using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    internal class MultipleItemHelperReceiver : Receiver
    {
        public MultipleItemHelperReceiver(IPortReceive port, MultipleItemReceiver parent) 
            : base(false, port, null) =>
            _parent = parent;

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask) 
            => _parent.Evaluate((int)_arbiterContext, messageNode, ref deferredTask);

        private readonly MultipleItemReceiver _parent;
    }
}
