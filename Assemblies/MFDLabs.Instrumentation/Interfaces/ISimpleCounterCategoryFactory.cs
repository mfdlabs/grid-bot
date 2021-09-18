using System.Collections.Generic;

namespace MFDLabs.Instrumentation.LegacySupport
{
    public interface ISimpleCounterCategoryFactory
    {
        ISimpleCounterCategory CreateSimpleCounterCategory(string categoryName, ICollection<string> counterNames);
    }
}
