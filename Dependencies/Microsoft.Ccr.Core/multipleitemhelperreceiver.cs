using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    internal class MultipleItemHelperReceiver : Receiver
    {
        public MultipleItemHelperReceiver(IPortReceive port, MultipleItemReceiver parent) : base(false, port, null)
        {
            this._parent = parent;
        }

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask)
        {
            return this._parent.Evaluate((int)this._arbiterContext, messageNode, ref deferredTask);
        }

        private MultipleItemReceiver _parent;
    }
}
