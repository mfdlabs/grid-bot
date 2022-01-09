using System.Collections.Generic;

namespace Microsoft.Ccr.Core
{
    internal class IteratorContext
    {
        public IteratorContext(IEnumerator<ITask> iterator, CausalityThreadContext causalities)
        {
            Iterator = iterator;
            Causalities = causalities;
        }

        internal readonly IEnumerator<ITask> Iterator;
        internal readonly CausalityThreadContext Causalities;
    }
}
