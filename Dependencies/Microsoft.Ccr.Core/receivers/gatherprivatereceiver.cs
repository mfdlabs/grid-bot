using Microsoft.Ccr.Core.Arbiters;

namespace Microsoft.Ccr.Core
{
    internal class GatherPrivateReceiver : Receiver
    {
        public GatherPrivateReceiver(IPortReceive port, MultipleItemGather parent) 
            : base(true, port, null) 
            => _parent = parent;

        public override bool Evaluate(IPortElement messageNode, ref ITask deferredTask) 
            => _parent.Evaluate(messageNode.Item, ref deferredTask);

        private readonly MultipleItemGather _parent;
    }
}
