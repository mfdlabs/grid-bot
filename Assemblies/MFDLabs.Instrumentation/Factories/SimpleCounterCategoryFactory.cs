using System;
using System.Collections.Generic;

namespace MFDLabs.Instrumentation.LegacySupport
{
    public class SimpleCounterCategoryFactory : ISimpleCounterCategoryFactory
    {
        public SimpleCounterCategoryFactory(ICounterRegistry counterRegistry) 
            => _CounterRegistry = counterRegistry ?? throw new ArgumentNullException("counterRegistry");

        public ISimpleCounterCategory CreateSimpleCounterCategory(string categoryName, ICollection<string> counterNames) 
            => new SimpleCounterCategory(_CounterRegistry, categoryName, counterNames);

        private readonly ICounterRegistry _CounterRegistry;
    }
}
