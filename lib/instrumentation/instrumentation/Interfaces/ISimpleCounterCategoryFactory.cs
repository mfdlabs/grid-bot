using System.Collections.Generic;

namespace Instrumentation.LegacySupport
{
    public interface ISimpleCounterCategoryFactory
    {
        ISimpleCounterCategory CreateSimpleCounterCategory(string categoryName, ICollection<string> counterNames);
    }
}
