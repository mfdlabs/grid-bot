using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    internal class IteratorContext
    {
        public IteratorContext(IEnumerator<ITask> iterator, CausalityThreadContext causalities)
        {
            this._iterator = iterator;
            this._causalities = causalities;
        }

        internal IEnumerator<ITask> _iterator;

        internal CausalityThreadContext _causalities;
    }
}
